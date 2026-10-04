import { PaneStateKind, usePaneStore } from '../store/paneStore'
import { formatCommandDuration } from '../terminal/commandNotices'
import { commandTracking } from '../terminal/commandOutput'
import { textArgument, type McpArguments } from './mcpArguments'
import { describeCommand, type McpCommand } from './mcpCommands'
import { locationOf, requireStartedPane, type McpPaneTarget } from './mcpPanes'
import { outputSettled } from './mcpWaitFor'

const START_TIMEOUT_MS = 3000
const STOP_TIMEOUT_MS = 3000
const POLL_MS = 50
const ENTER = '\r'
const CTRL_C = '\x03'
const ALTERNATE_BUFFER = 'alternate'

export enum RunOutcome {
  Running = 'running',
  Finished = 'finished',
  Sent = 'sent',
}

interface McpRunResult {
  pane: string
  outcome: RunOutcome
  message: string
  command?: McpCommand
  startedAt?: string
}

interface McpInterruptResult {
  pane: string
  stopped?: boolean
  message: string
}

const pollUntil = (condition: () => boolean, timeoutMs: number): Promise<boolean> =>
  new Promise((resolve) => {
    const begun = Date.now()
    const timer = setInterval(() => {
      const met = condition()
      if (met || Date.now() - begun >= timeoutMs) {
        clearInterval(timer)
        resolve(met)
      }
    }, POLL_MS)
  })

export const requireOwned = (target: McpPaneTarget, caller: string | undefined, action: string): void => {
  if (!caller || target.pane.owner !== caller) {
    throw new Error(`Le pane ${target.pane.id} (${locationOf(target)}) n’a pas été créé par vous : ${action} demande l’accord de l’utilisateur.`)
  }
}

const requireReady = (target: McpPaneTarget): void => {
  if (usePaneStore.getState().states[target.pane.id]?.kind === PaneStateKind.Exited) {
    throw new Error(`Le shell du pane ${target.pane.id} (${locationOf(target)}) s’est terminé : relancez-le depuis Tily ou ouvrez un autre pane.`)
  }
  if (target.handle.terminal.buffer.active.type === ALTERNATE_BUFFER) {
    throw new Error(`Le pane ${target.pane.id} (${locationOf(target)}) affiche un programme plein écran : rien n’y a été écrit.`)
  }
  const { running } = commandTracking(target.handle.terminal)
  if (running) {
    throw new Error(`Une commande tourne déjà dans le pane ${target.pane.id} (« ${running.command} ») : arrêtez-la avec tily_interrupt ou ouvrez un autre pane.`)
  }
}

export const runCommand = async (values: McpArguments, caller: string | undefined): Promise<McpRunResult> => {
  const target = requireStartedPane(textArgument(values, 'pane'))
  requireOwned(target, caller, 'y écrire')
  const command = textArgument(values, 'command')
  if (!command) {
    throw new Error('Indiquez la commande à lancer.')
  }
  requireReady(target)
  const { terminal } = target.handle
  const before = commandTracking(terminal)
  const lastId = before.finished.at(-1)?.id ?? 0
  terminal.input(command + ENTER, false)
  if (!before.integrated) {
    return { pane: target.pane.id, outcome: RunOutcome.Sent, message: 'Commande envoyée. Ce shell ne signale pas ses commandes : suivez-la avec tily_wait_for (pattern) et tily_read_pane.' }
  }
  const newlyFinished = () => commandTracking(terminal).finished.find((candidate) => candidate.id > lastId)
  await pollUntil(() => commandTracking(terminal).running !== null || newlyFinished() !== undefined, START_TIMEOUT_MS)
  const finished = newlyFinished()
  if (finished) {
    await outputSettled(terminal)
    return {
      pane: target.pane.id,
      outcome: RunOutcome.Finished,
      command: describeCommand(target, finished, true),
      message: `La commande s’est terminée ${finished.success ? 'avec succès' : 'en échec'} après ${formatCommandDuration(finished.durationMs)}.`,
    }
  }
  const { running } = commandTracking(terminal)
  return running
    ? { pane: target.pane.id, outcome: RunOutcome.Running, startedAt: new Date(running.startedAt).toISOString(), message: 'La commande a démarré : suivez-la avec tily_wait_for et tily_read_pane.' }
    : { pane: target.pane.id, outcome: RunOutcome.Sent, message: 'Commande envoyée, mais son démarrage n’a pas été signalé dans les 3 s : vérifiez avec tily_read_pane.' }
}

export const interruptPane = async (values: McpArguments, caller: string | undefined): Promise<McpInterruptResult> => {
  const target = requireStartedPane(textArgument(values, 'pane'))
  requireOwned(target, caller, 'l’interrompre')
  const { terminal } = target.handle
  const before = commandTracking(terminal)
  terminal.input(CTRL_C, false)
  if (!before.integrated) {
    return { pane: target.pane.id, message: 'Ctrl + C envoyé. Ce shell ne signale pas ses commandes : vérifiez avec tily_read_pane.' }
  }
  if (!before.running) {
    return { pane: target.pane.id, stopped: true, message: 'Ctrl + C envoyé ; aucune commande ne tournait.' }
  }
  const stopped = await pollUntil(() => commandTracking(terminal).running === null, STOP_TIMEOUT_MS)
  return {
    pane: target.pane.id,
    stopped,
    message: stopped
      ? `La commande « ${before.running.command} » s’est arrêtée.`
      : `Ctrl + C envoyé, mais la commande « ${before.running.command} » tourne toujours : elle ignore peut-être Ctrl + C.`,
  }
}
