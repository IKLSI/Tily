import { nextPaneInState } from '../agents/agentSummary'
import { AgentState } from '../bridge/messages'
import { activeTab, activeWorkspace, RightPanelView } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useGitStore } from '../store/gitStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { terminalRegistry } from './terminalRegistry'

const COPY_FAILED = 'Copie dans le presse-papiers impossible.'
const PASTE_FAILED = 'Lecture du presse-papiers impossible.'
const NO_MOUSE_TRACKING = 'none'
const OVERLAY_DEFAULT_SELECTOR = '[data-overlay-default]'
const TABBABLE = 0
const UNTABBABLE = -1
const VIEWPORT_SELECTOR = '.xterm-viewport'
const TAB_INDEX_ATTRIBUTE = 'tabindex'

const reportFailure = (message: string) => (): void => useHostStore.getState().setStatus(message, StatusLevel.Error)

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
  const workspace = activeWorkspace(session)
  const tabs = session.workspaces.flatMap((candidate) => candidate.tabs)
  const paneId = nextPaneInState(tabs, useAgentStore.getState().agents, AgentState.Waiting, workspace ? activeTab(workspace).active : undefined)
  if (paneId) {
    joinPane(paneId)
  } else {
    useHostStore.getState().setStatus(NO_WAITING_AGENT_STATUS)
  }
}
