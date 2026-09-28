import { bridge } from '../bridge/bridge'
import { useHostStore } from '../store/hostStore'
import { activePane, activeTab, activeWorkspace, DEFAULT_SHELL, RightPanelView, SplitAxis, type Workspace } from '../model/session'
import { Direction, paneInDirection } from '../components/paneNavigation'
import { useSessionStore } from '../store/sessionStore'
import { requestApplicationClose } from '../terminal/closeGuard'
import { closePaneKeepingText, restoreClosedTab } from '../terminal/tabLifecycle'
import { togglePanelView } from '../panel/rightPanel'
import { RenameOrigin, useUiStore } from '../store/uiStore'

const LEADER_TIMEOUT_MS = 5000
const MODIFIER_KEYS = new Set(['Control', 'Shift', 'Alt', 'AltGraph', 'Meta'])
const CANCEL_KEY = 'Escape'
const LEADER_EXPIRED_STATUS = 'Leader expiré : la saisie revient au terminal.'

export interface LeaderHint {
  keys: string
  label: string
}

export const LEADER_HINTS: LeaderHint[] = [
  { keys: 'T', label: 'onglet' },
  { keys: 'V', label: 'côte à côte' },
  { keys: 'H', label: 'haut / bas' },
  { keys: 'W', label: 'workspace' },
  { keys: 'F', label: 'projet' },
  { keys: 'E', label: 'fichiers' },
  { keys: 'G', label: 'git' },
  { keys: 'X', label: 'fermer le pane' },
  { keys: 'Z', label: 'rouvrir' },
  { keys: 'P', label: 'palette' },
  { keys: ',', label: 'paramètres' },
  { keys: '← ↑ → ↓', label: 'pane voisin' },
  { keys: 'PgUp / PgDn', label: 'déplacer l’onglet' },
  { keys: 'Échap', label: 'annuler' },
  { keys: 'Ctrl + Espace', label: 'envoyer au terminal' },
]

export enum Command {
  Palette = 'palette',
  NewTab = 'newTab',
  SplitSideBySide = 'splitSideBySide',
  SplitTopBottom = 'splitTopBottom',
  NewWorkspace = 'newWorkspace',
  Projects = 'projects',
  Settings = 'settings',
  ClosePane = 'closePane',
  FocusPaneLeft = 'focusPaneLeft',
  FocusPaneRight = 'focusPaneRight',
  FocusPaneUp = 'focusPaneUp',
  FocusPaneDown = 'focusPaneDown',
  MoveTabLeft = 'moveTabLeft',
  MoveTabRight = 'moveTabRight',
  RestoreTab = 'restoreTab',
  ToggleExplorer = 'toggleExplorer',
  ToggleGit = 'toggleGit',
}

const LEADER_KEYS: Record<string, Command> = {
  p: Command.Palette,
  t: Command.NewTab,
  v: Command.SplitSideBySide,
  h: Command.SplitTopBottom,
  w: Command.NewWorkspace,
  f: Command.Projects,
  e: Command.ToggleExplorer,
  g: Command.ToggleGit,
  ',': Command.Settings,
  x: Command.ClosePane,
  z: Command.RestoreTab,
  ArrowRight: Command.FocusPaneRight,
  ArrowDown: Command.FocusPaneDown,
  ArrowLeft: Command.FocusPaneLeft,
  ArrowUp: Command.FocusPaneUp,
  PageUp: Command.MoveTabLeft,
  PageDown: Command.MoveTabRight,
}

const DIRECT_PAGE_KEYS: Record<string, Command> = {
  PageUp: Command.MoveTabLeft,
  PageDown: Command.MoveTabRight,
}

const DIRECT_LETTER_KEYS: Record<string, Command> = {
  p: Command.Palette,
  t: Command.NewTab,
  d: Command.SplitSideBySide,
  h: Command.SplitTopBottom,
  w: Command.NewWorkspace,
  x: Command.ClosePane,
  z: Command.RestoreTab,
  e: Command.ToggleExplorer,
  g: Command.ToggleGit,
}

const DIRECT_ARROW_KEYS: Record<string, Command> = {
  ArrowRight: Command.FocusPaneRight,
  ArrowDown: Command.FocusPaneDown,
  ArrowLeft: Command.FocusPaneLeft,
  ArrowUp: Command.FocusPaneUp,
}

let leaderTimer: ReturnType<typeof setTimeout> | undefined

const letterOf = (event: KeyboardEvent): string => {
  if (/^[a-zA-Z]$/.test(event.key)) {
    return event.key.toLowerCase()
  }
  const fromCode = /^Key([A-Z])$/.exec(event.code)
  return fromCode ? fromCode[1].toLowerCase() : event.key.toLowerCase()
}

const isLeaderChord = (event: KeyboardEvent): boolean =>
  event.ctrlKey && !event.altKey && !event.shiftKey && (event.key === ' ' || event.code === 'Space')

const isCloseWindow = (event: KeyboardEvent): boolean => event.altKey && event.key === 'F4'
const isCopy = (event: KeyboardEvent): boolean => event.ctrlKey && event.shiftKey && !event.altKey && letterOf(event) === 'c'
const isPlainCtrlC = (event: KeyboardEvent): boolean => event.ctrlKey && !event.shiftKey && !event.altKey && letterOf(event) === 'c'
const isPaste = (event: KeyboardEvent): boolean => event.ctrlKey && !event.altKey && letterOf(event) === 'v'

const directCommand = (event: KeyboardEvent): Command | undefined => {
  if (event.ctrlKey && !event.altKey && letterOf(event) === 'p') {
    return Command.Palette
  }
  if (event.ctrlKey && event.shiftKey && !event.altKey) {
    return DIRECT_PAGE_KEYS[event.key] ?? DIRECT_LETTER_KEYS[letterOf(event)]
  }
  if (event.altKey && !event.ctrlKey && !event.shiftKey) {
    return DIRECT_ARROW_KEYS[event.key]
  }
  return undefined
}

const exitLeader = (): void => {
  clearTimeout(leaderTimer)
  useHostStore.getState().setLeaderActive(false)
}

const expireLeader = (): void => {
  exitLeader()
  useHostStore.getState().setStatus(LEADER_EXPIRED_STATUS)
}

const enterLeader = (): void => {
  useHostStore.getState().setLeaderActive(true)
  clearTimeout(leaderTimer)
  leaderTimer = setTimeout(expireLeader, LEADER_TIMEOUT_MS)
}

const leaderKeyOf = (event: KeyboardEvent): string => (event.key.length === 1 ? event.key.toLowerCase() : event.key)

const decideInLeader = (event: KeyboardEvent): boolean => {
  if (MODIFIER_KEYS.has(event.key)) {
    return false
  }
  exitLeader()
  if (isLeaderChord(event)) {
    return true
  }
  if (event.key === CANCEL_KEY) {
    return false
  }
  const command = LEADER_KEYS[leaderKeyOf(event)]
  if (command) {
    runCommand(command)
    return false
  }
  return true
}

const currentWorkspace = (): Workspace | undefined => {
  const { session } = useSessionStore.getState()
  return session ? activeWorkspace(session) : undefined
}

const currentShell = (): string => {
  const workspace = currentWorkspace()
  return workspace ? activePane(activeTab(workspace)).shell : DEFAULT_SHELL
}

const currentPaneId = (): string => {
  const workspace = currentWorkspace()
  return workspace ? activeTab(workspace).active : ''
}

const focusPaneToward = (direction: Direction): void => {
  const target = paneInDirection(currentPaneId(), direction)
  if (target) {
    useSessionStore.getState().selectPane(target)
  }
}

export const runCommand = (command: Command): void => {
  const sessionStore = useSessionStore.getState()
  const hostStore = useHostStore.getState()
  switch (command) {
    case Command.Palette:
      useUiStore.getState().openPalette()
      break
    case Command.NewTab:
      sessionStore.newTab(currentShell())
      break
    case Command.SplitSideBySide:
      sessionStore.splitPane(SplitAxis.Horizontal)
      break
    case Command.SplitTopBottom:
      sessionStore.splitPane(SplitAxis.Vertical)
      break
    case Command.NewWorkspace:
      useUiStore.getState().startRenamingWorkspace(sessionStore.newWorkspace(`Workspace ${(sessionStore.session?.workspaces.length ?? 0) + 1}`, hostStore.home, DEFAULT_SHELL), RenameOrigin.Header)
      break
    case Command.Settings:
      bridge.send({ type: 'settings.get' })
      useUiStore.getState().openSettings()
      break
    case Command.Projects:
      bridge.send({ type: 'projects.list' })
      useUiStore.getState().openProjectPicker()
      break
    case Command.ClosePane:
      closePaneKeepingText(currentPaneId())
      break
    case Command.FocusPaneLeft:
      focusPaneToward(Direction.Left)
      break
    case Command.FocusPaneRight:
      focusPaneToward(Direction.Right)
      break
    case Command.FocusPaneUp:
      focusPaneToward(Direction.Up)
      break
    case Command.FocusPaneDown:
      focusPaneToward(Direction.Down)
      break
    case Command.MoveTabLeft:
      sessionStore.moveActiveTab(-1)
      break
    case Command.MoveTabRight:
      sessionStore.moveActiveTab(1)
      break
    case Command.RestoreTab:
      restoreClosedTab()
      break
    case Command.ToggleExplorer:
      togglePanelView(RightPanelView.Files, true)
      break
    case Command.ToggleGit:
      togglePanelView(RightPanelView.Git, true)
      break
  }
}

export const isReservedShortcut = (event: KeyboardEvent): boolean =>
  isLeaderChord(event) || isCloseWindow(event) || isCopy(event) || isPaste(event) || directCommand(event) !== undefined

export interface ShortcutActions {
  hasSelection: () => boolean
  copySelection: () => void
  pasteClipboard: () => void
}

export const handleTerminalKey = (event: KeyboardEvent, actions: ShortcutActions): boolean => {
  const passToTerminal = decide(event, actions)
  if (!passToTerminal) {
    event.preventDefault()
    event.stopPropagation()
  }
  return passToTerminal
}

const decide = (event: KeyboardEvent, actions: ShortcutActions): boolean => {
  if (event.isComposing) {
    return true
  }
  if (event.type !== 'keydown') {
    return !isReservedShortcut(event)
  }
  if (isCloseWindow(event)) {
    requestApplicationClose()
    return false
  }
  if (useHostStore.getState().leaderActive) {
    return decideInLeader(event)
  }
  if (isLeaderChord(event)) {
    enterLeader()
    return false
  }
  if (isCopy(event) || (isPlainCtrlC(event) && actions.hasSelection())) {
    actions.copySelection()
    return false
  }
  if (isPaste(event)) {
    actions.pasteClipboard()
    return false
  }
  const command = directCommand(event)
  if (command) {
    runCommand(command)
    return false
  }
  return true
}
