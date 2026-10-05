import type { IMarker, Terminal } from '@xterm/xterm'
import { PaneStateKind, usePaneStore } from '../store/paneStore'
import { formatCommandDuration } from '../terminal/commandNotices'
import { commandTracking, nextOutputRow, onCommandFinished, type FinishedCommand } from '../terminal/commandOutput'
import { terminalRegistry } from '../terminal/terminalRegistry'
import { lastContentRow, logicalLinesBetween, recentText } from '../terminal/terminalText'
import { flagArgument, numberArgument, textArgument, type McpArguments } from './mcpArguments'
import { describeCommand, type McpCommand } from './mcpCommands'
import { requireStartedPane, type McpPaneTarget } from './mcpPanes'

const DEFAULT_TIMEOUT_SECONDS = 60
const SECOND_MS = 1000
const POLL_MS = 200
const SETTLE_POLL_MS = 50
const SETTLE_QUIET_MS = 250
const SETTLE_MAX_MS = 1500
const TAIL_LINES = 20
const ALTERNATE_BUFFER = 'alternate'
const REGEX_SPECIALS = /[.*+?^${}()|[\]\\]/g
const UNTRACKED_END =
  'Ce shell ne signale pas la fin de ses commandes à Tily (seuls Windows PowerShell et PowerShell 7 le font) : attendez plutôt un texte avec pattern.'
const EXITED = 'Le shell du pane s’est terminé.'
const CLOSED = 'Le pane a été fermé pendant l’attente.'

export enum WaitOutcome {
  Matched = 'matched',
  CommandFinished = 'commandFinished',
  NotRunning = 'notRunning',
  Exited = 'exited',
  Closed = 'closed',
  Timeout = 'timeout',
}

interface McpWaitResult {
  pane: string
  outcome: WaitOutcome
  message: string
  waitedMs: number
  line?: string
  command?: McpCommand
  tail?: string
}

type WaitEnd = Omit<McpWaitResult, 'pane' | 'waitedMs'>

interface Scanner {
  find: () => string | null
  dispose: () => void
}

const matcherOf = (pattern: string, regex: boolean): RegExp => {
  try {
    return new RegExp(regex ? pattern : pattern.replace(REGEX_SPECIALS, '\\$&'), 'i')
  } catch (error) {
    throw new Error(`Expression régulière invalide : ${error instanceof Error ? error.message : String(error)}`)
  }
}

const outcomeLabel = (command: FinishedCommand): string => (command.success ? 'avec succès' : 'en échec')

const createScanner = (terminal: Terminal, fromRow: number, matcher: RegExp): Scanner => {
  const normal = terminal.buffer.normal
  let anchor: IMarker | null = null
  let anchorRow = fromRow
  let skippedRows = 0
  const place = (row: number): void => {
    anchor?.dispose()
    anchor = null
    anchorRow = row
    if (terminal.buffer.active === normal) {
      const target = Math.min(row, normal.length - 1)
      skippedRows = row - target
      anchor = terminal.registerMarker(target - (normal.baseY + normal.cursorY)) ?? null
    }
  }
  const scanFrom = (): number => {
    if (!anchor) {
      place(anchorRow)
    }
    if (!anchor) {
      return anchorRow
    }
    return anchor.isDisposed || anchor.line < 0 ? 0 : anchor.line + skippedRows
  }
  const find = (): string | null => {
    const buffer = terminal.buffer.active
    if (buffer.type === ALTERNATE_BUFFER) {
      return logicalLinesBetween(buffer, 0, buffer.length - 1).find((line) => matcher.test(line.text))?.text.trim() ?? null
    }
    const from = scanFrom()
    const lines = logicalLinesBetween(buffer, from, lastContentRow(buffer)).filter((line) => line.row >= from)
    const found = lines.find((line) => matcher.test(line.text))
    if (found) {
      return found.text.trim()
    }
    const last = lines.at(-1)
    if (last) {
      place(last.row)
    }
    return null
  }
  place(fromRow)
  return { find, dispose: () => anchor?.dispose() }
}

export const outputSettled = (terminal: Terminal): Promise<void> =>
  new Promise((resolve) => {
    const begun = Date.now()
    let lastWrite = begun
    const subscription = terminal.onWriteParsed(() => {
      lastWrite = Date.now()
    })
    const timer = setInterval(() => {
      const now = Date.now()
      if (now - lastWrite >= SETTLE_QUIET_MS || now - begun >= SETTLE_MAX_MS) {
        clearInterval(timer)
        subscription.dispose()
        resolve()
      }
    }, SETTLE_POLL_MS)
  })

const watch = (target: McpPaneTarget, matcher: RegExp | null, timeoutMs: number): Promise<McpWaitResult> =>
  new Promise((resolve) => {
    const { terminal } = target.handle
    const startedAt = Date.now()
    const scanner = matcher ? createScanner(terminal, nextOutputRow(terminal) ?? terminal.buffer.normal.baseY, matcher) : null
    const stops: (() => void)[] = []
    let done = false
    const waited = (): number => Date.now() - startedAt
    const end = (result: WaitEnd): void => {
      if (done) {
        return
      }
      done = true
      stops.forEach((stop) => stop())
      scanner?.dispose()
      resolve({ pane: target.pane.id, waitedMs: waited(), ...result })
    }
    const closed = (): boolean => terminalRegistry.get(target.pane.id) !== target.handle
    const exited = (): boolean => usePaneStore.getState().states[target.pane.id]?.kind === PaneStateKind.Exited
    const tail = (): string => recentText(terminal, TAIL_LINES)
    const matched = (): boolean => {
      const line = scanner?.find() ?? null
      if (line !== null) {
        end({ outcome: WaitOutcome.Matched, line, message: `Texte trouvé après ${formatCommandDuration(waited())}.` })
      }
      return line !== null
    }
    const check = (): boolean => {
      if (closed()) {
        end({ outcome: WaitOutcome.Closed, message: CLOSED })
        return true
      }
      if (matched()) {
        return true
      }
      if (exited()) {
        end({ outcome: WaitOutcome.Exited, message: EXITED, tail: tail() })
        return true
      }
      return false
    }
    const timeOut = (): void => {
      if (check()) {
        return
      }
      const running = commandTracking(terminal).running
      const message = matcher
        ? `Délai de ${formatCommandDuration(timeoutMs)} écoulé sans que le texte attendu s’affiche.`
        : `Délai de ${formatCommandDuration(timeoutMs)} écoulé : la commande « ${running?.command ?? ''} » tourne toujours.`
      end({ outcome: WaitOutcome.Timeout, message, tail: tail() })
    }
    const commandFinished = (command: FinishedCommand): void => {
      void outputSettled(terminal).then(() => {
        if (done || check()) {
          return
        }
        const message = matcher
          ? `La commande « ${command.command} » s’est terminée ${outcomeLabel(command)} sans afficher le texte attendu.`
          : `La commande « ${command.command} » s’est terminée ${outcomeLabel(command)} après ${formatCommandDuration(command.durationMs)}.`
        end({ outcome: WaitOutcome.CommandFinished, command: describeCommand(target, command, true), message })
      })
    }
    if (check()) {
      return
    }
    const poll = setInterval(check, POLL_MS)
    const timer = setTimeout(timeOut, timeoutMs)
    stops.push(() => clearInterval(poll), () => clearTimeout(timer), onCommandFinished(terminal, commandFinished))
  })

export const waitFor = async (values: McpArguments): Promise<McpWaitResult> => {
  const target = requireStartedPane(textArgument(values, 'pane'))
  const pattern = textArgument(values, 'pattern')
  const matcher = pattern === undefined ? null : matcherOf(pattern, flagArgument(values, 'regex'))
  const timeoutMs = (numberArgument(values, 'timeoutSeconds') ?? DEFAULT_TIMEOUT_SECONDS) * SECOND_MS
  const tracking = commandTracking(target.handle.terminal)
  if (!matcher && !tracking.integrated) {
    throw new Error(UNTRACKED_END)
  }
  if (!matcher && !tracking.running) {
    const last = tracking.finished.at(-1)
    return {
      pane: target.pane.id,
      outcome: WaitOutcome.NotRunning,
      message: last ? `Aucune commande en cours ; la dernière, « ${last.command} », s’est terminée ${outcomeLabel(last)}.` : 'Aucune commande en cours dans ce pane.',
      waitedMs: 0,
      command: last ? describeCommand(target, last, true) : undefined,
    }
  }
  return watch(target, matcher, timeoutMs)
}
