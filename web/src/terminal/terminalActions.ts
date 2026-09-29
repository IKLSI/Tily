import { longestWaitingFirst, waitingPanes } from '../agents/agentSummary'
import { activeTab, activeWorkspace, RightPanelView } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useGitStore } from '../store/gitStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { CommandDirection, lastCommandOutput, OutputFailure, scrollToCommand } from './commandOutput'
import { terminalRegistry } from './terminalRegistry'

const COPY_FAILED = 'Copie dans le presse-papiers impossible.'
const PASTE_FAILED = 'Lecture du presse-papiers impossible.'
const NO_MOUSE_TRACKING = 'none'
const OVERLAY_DEFAULT_SELECTOR = '[data-overlay-default]'
const TABBABLE = 0
const UNTABBABLE = -1
const VIEWPORT_SELECTOR = '.xterm-viewport'
const TAB_INDEX_ATTRIBUTE = 'tabindex'
const AGENT_LINE_BREAK = '\x1b\r'
const LINE_BREAK = '\n'

const reportFailure = (message: string) => (): void => useHostStore.getState().setStatus(message, StatusLevel.Error)

const OUTPUT_FAILURES: Record<OutputFailure, string> = {
  [OutputFailure.NoCommand]: 'Aucune commande terminée dans ce terminal depuis son ouverture (Windows PowerShell et PowerShell 7 uniquement).',
  [OutputFailure.Trimmed]: 'La sortie de la dernière commande n’est plus dans l’historique du terminal.',
  [OutputFailure.Empty]: 'La dernière commande n’a rien affiché.',
}

const lineCountLabel = (text: string): string => {
  const count = text.split(LINE_BREAK).length
  return count === 1 ? '1 ligne' : `${count} lignes`
}

export const copyLastCommandOutput = (paneId: string): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (!terminal) {
    return
  }
  const output = lastCommandOutput(terminal)
  if ('failure' in output) {
    useHostStore.getState().setStatus(OUTPUT_FAILURES[output.failure])
    return
  }
  void navigator.clipboard
    .writeText(output.text)
    .then(() => useHostStore.getState().setStatus(`Sortie de la dernière commande copiée (${lineCountLabel(output.text)}).`))
    .catch(reportFailure(COPY_FAILED))
}

const NO_COMMAND_ABOVE = 'Aucune commande plus haut dans ce terminal (Windows PowerShell et PowerShell 7 uniquement).'

export const scrollPaneToCommand = (paneId: string, direction: CommandDirection): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (terminal && !scrollToCommand(terminal, direction) && direction === CommandDirection.Previous) {
    useHostStore.getState().setStatus(NO_COMMAND_ABOVE)
  }
}

export const hasPaneSelection = (paneId: string): boolean => terminalRegistry.get(paneId)?.terminal.hasSelection() ?? false

export const copyPaneSelection = (paneId: string): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (terminal?.hasSelection()) {
    void navigator.clipboard.writeText(terminal.getSelection()).catch(reportFailure(COPY_FAILED))
    terminal.clearSelection()
  }
}

export const pasteIntoPane = (paneId: string): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (terminal) {
    void navigator.clipboard
      .readText()
      .then((text) => terminal.paste(text))
      .catch(reportFailure(PASTE_FAILED))
  }
}

export const hasPaneAgent = (paneId: string): boolean => paneId in useAgentStore.getState().agents

export const insertAgentLineBreak = (paneId: string): void => terminalRegistry.get(paneId)?.terminal.input(AGENT_LINE_BREAK)

export const selectAllInPane = (paneId: string): void => terminalRegistry.get(paneId)?.terminal.selectAll()

export const isMouseTrackedByProgram = (paneId: string): boolean => (terminalRegistry.get(paneId)?.terminal.modes.mouseTrackingMode ?? NO_MOUSE_TRACKING) !== NO_MOUSE_TRACKING

const overlayDefaultOf = (paneId: string): HTMLElement | null =>
  document.querySelector<HTMLElement>(`[data-pane-id="${CSS.escape(paneId)}"] ${OVERLAY_DEFAULT_SELECTOR}`)

export const focusPane = (paneId: string): void => {
  const overlayDefault = overlayDefaultOf(paneId)
  if (overlayDefault) {
    overlayDefault.focus()
  } else {
    terminalRegistry.get(paneId)?.terminal.focus()
  }
}

export const setPaneTerminalTabbable = (paneId: string, tabbable: boolean): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  const viewport = terminal?.element?.querySelector<HTMLElement>(VIEWPORT_SELECTOR)
  if (terminal?.textarea) {
    terminal.textarea.tabIndex = tabbable ? TABBABLE : UNTABBABLE
  }
  if (viewport && tabbable) {
    viewport.removeAttribute(TAB_INDEX_ATTRIBUTE)
  } else if (viewport) {
    viewport.tabIndex = UNTABBABLE
  }
}

export const insertIntoPane = (paneId: string, text: string): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (terminal && text.length > 0) {
    useSessionStore.getState().selectPane(paneId)
    terminal.paste(text)
    terminal.focus()
  }
}

const activeTabShowsGit = (): boolean => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  const tab = workspace ? activeTab(workspace) : undefined
  return Boolean(tab?.explorer) && tab?.panel === RightPanelView.Git
}

export const joinPane = (paneId: string): void => {
  useAgentStore.getState().acknowledge(paneId)
  useSessionStore.getState().selectPane(paneId)
  if (activeTabShowsGit()) {
    useGitStore.getState().setGraphOpen(false)
  }
  focusPane(paneId)
}

const NO_WAITING_AGENT_STATUS = 'Aucun agent en attente.'

export const joinNextWaitingPane = (): void => {
  const { session } = useSessionStore.getState()
  if (!session) {
    return
  }
  const { agents, since } = useAgentStore.getState()
  const ordered = longestWaitingFirst(waitingPanes(session, agents), since, Date.now()).map((pane) => pane.paneId)
  const workspace = activeWorkspace(session)
  const paneId = ordered[(ordered.indexOf(workspace ? activeTab(workspace).active : '') + 1) % ordered.length]
  if (paneId) {
    joinPane(paneId)
  } else {
    useHostStore.getState().setStatus(NO_WAITING_AGENT_STATUS)
  }
}
