import { activePane, activeTab, createOwnedPane, createOwnedTab, createOwnedWorkspace, DEFAULT_SHELL, folderName, panesOf, splitLeaf, SplitAxis, type Pane, type Tab, type Workspace } from '../../model/session'
import { useHostStore } from '../../stores/hostStore'
import { useSessionStore } from '../../stores/sessionStore'
import { terminalRegistry } from '../terminal/terminalRegistry'
import { flagArgument, textArgument, type McpArguments } from './mcpArguments'
import { locationOf, placeOf, requirePlace, requireSession, requireTab, requireWorkspace } from './mcpPanes'

enum SplitDirection {
  Right = 'right',
  Down = 'down',
}

const AXES: Record<SplitDirection, SplitAxis> = {
  [SplitDirection.Right]: SplitAxis.Horizontal,
  [SplitDirection.Down]: SplitAxis.Vertical,
}

interface McpElement {
  id: string
  name: string
}

interface McpCreated {
  workspace: McpElement
  tab: McpElement
  pane: { id: string; path: string; shell: string }
  shown: boolean
  command?: string
  message: string
}

interface McpShown {
  workspace: McpElement
  tab: McpElement
  pane: string
  message: string
}

export const requireCaller = (caller: string | undefined): string => {
  if (!caller) {
    throw new Error('Pane appelant inconnu : lancez Claude Code depuis un pane de Tily.')
  }
  return caller
}

const requireShell = (shell: string | undefined, fallback: string): string => {
  const id = shell ?? fallback
  const { shells } = useHostStore.getState()
  const profile = shells.find((candidate) => candidate.id === id)
  if (!profile?.available) {
    const available = shells.filter((candidate) => candidate.available).map((candidate) => candidate.id)
    throw new Error(`Shell ${profile ? 'indisponible' : 'inconnu'} : ${id}. Shells disponibles : ${available.join(', ')}.`)
  }
  return id
}

const isDisplayed = (tabId: string): boolean => {
  const session = requireSession()
  const workspace = session.workspaces.find((candidate) => candidate.id === session.active)
  return workspace?.active === tabId
}

const created = (workspace: Workspace, tab: Tab, pane: Pane, command: string | undefined, what: string): McpCreated => {
  const shown = isDisplayed(tab.id)
  const where = `« ${workspace.name} › ${tab.name} »`
  const message = shown
    ? `${what} ${where}${command ? ` : la commande « ${command} » démarre dès l’invite.` : ', son shell démarre.'}`
    : `${what} ${where} sans changer l’affichage : son shell démarrera à son premier affichage (tily_focus).`
  return {
    workspace: { id: workspace.id, name: workspace.name },
    tab: { id: tab.id, name: tab.name },
    pane: { id: pane.id, path: pane.path, shell: pane.shell },
    shown,
    command,
    message,
  }
}

const prepareStart = (pane: Pane, command: string | undefined): void => {
  if (command) {
    terminalRegistry.runAtStart(pane.id, command)
  }
}

export const openWorkspace = (values: McpArguments, caller: string | undefined): McpCreated => {
  const owner = requireCaller(caller)
  const path = textArgument(values, 'path')
  if (!path) {
    throw new Error('Indiquez le dossier du workspace.')
  }
  const command = textArgument(values, 'command')
  const show = command !== undefined || flagArgument(values, 'focus')
  const workspace = createOwnedWorkspace(textArgument(values, 'name') ?? (folderName(path) || path), path, requireShell(textArgument(values, 'shell'), DEFAULT_SHELL), owner)
  const tab = workspace.tabs[0]
  const pane = activePane(tab)
  prepareStart(pane, command)
  useSessionStore.getState().change((draft) => {
    draft.workspaces.push(workspace)
    if (show) {
      draft.active = workspace.id
    }
  })
  return created(workspace, tab, pane, command, 'Workspace ouvert, avec l’onglet')
}

export const newTab = (values: McpArguments, caller: string | undefined): McpCreated => {
  const owner = requireCaller(caller)
  const session = requireSession()
  const callerPlace = placeOf(session, caller)
  const workspace = requireWorkspace(textArgument(values, 'workspace') ?? callerPlace?.workspace.id ?? session.active)
  const reference = callerPlace?.workspace.id === workspace.id ? callerPlace.pane : activePane(activeTab(workspace))
  const command = textArgument(values, 'command')
  const show = command !== undefined || flagArgument(values, 'focus')
  const name = textArgument(values, 'name')
  const fresh = createOwnedTab(textArgument(values, 'path') ?? reference.path, requireShell(textArgument(values, 'shell'), DEFAULT_SHELL), owner)
  const tab = name ? { ...fresh, name, manual: true } : fresh
  const pane = activePane(tab)
  prepareStart(pane, command)
  useSessionStore.getState().change((draft) => {
    const target = draft.workspaces.find((candidate) => candidate.id === workspace.id)
    if (target) {
      target.tabs.push(tab)
      if (show) {
        target.active = tab.id
        draft.active = target.id
      }
    }
  })
  return created(workspace, tab, pane, command, 'Onglet ouvert :')
}

const directionOf = (value: string | undefined): SplitDirection => {
  const direction = value ?? SplitDirection.Right
  if (!Object.values<string>(SplitDirection).includes(direction)) {
    throw new Error(`Direction inconnue : ${direction}. Utilisez right (côte à côte) ou down (haut / bas).`)
  }
  return direction as SplitDirection
}

export const splitPane = (values: McpArguments, caller: string | undefined): McpCreated => {
  const owner = requireCaller(caller)
  const place = requirePlace(textArgument(values, 'pane') ?? caller)
  const axis = AXES[directionOf(textArgument(values, 'direction'))]
  const command = textArgument(values, 'command')
  const focus = flagArgument(values, 'focus')
  const pane = createOwnedPane(textArgument(values, 'path') ?? place.pane.path, requireShell(textArgument(values, 'shell'), place.pane.shell), owner)
  prepareStart(pane, command)
  useSessionStore.getState().change((draft) => {
    const workspace = draft.workspaces.find((candidate) => candidate.id === place.workspace.id)
    const tab = workspace?.tabs.find((candidate) => candidate.id === place.tab.id)
    if (!workspace || !tab || !panesOf(tab.tree).some((candidate) => candidate.id === place.pane.id)) {
      return
    }
    tab.tree = splitLeaf(tab.tree, place.pane.id, axis, pane)
    if (focus) {
      tab.active = pane.id
    }
    if (focus || command !== undefined) {
      workspace.active = tab.id
      draft.active = workspace.id
    }
  })
  return created(place.workspace, place.tab, pane, command, 'Split ouvert dans')
}

const shownContext = (paneId: string): McpShown => {
  const place = requirePlace(paneId)
  return { workspace: { id: place.workspace.id, name: place.workspace.name }, tab: { id: place.tab.id, name: place.tab.name }, pane: place.pane.id, message: `« ${locationOf(place)} » est affiché.` }
}

const singleTarget = (values: McpArguments, names: string[], message: string): [string, string] => {
  const given = names.flatMap((name) => {
    const value = textArgument(values, name)
    return value ? [[name, value] as [string, string]] : []
  })
  if (given.length !== 1) {
    throw new Error(message)
  }
  return given[0]
}

export const focusElement = (values: McpArguments): McpShown => {
  const [kind, id] = singleTarget(values, ['pane', 'tab', 'workspace'], 'Indiquez un seul élément à afficher : pane, tab ou workspace.')
  const store = useSessionStore.getState()
  if (kind === 'pane') {
    requirePlace(id)
    store.selectPane(id)
    return shownContext(id)
  }
  if (kind === 'tab') {
    const { tab } = requireTab(id)
    store.selectPane(tab.active)
    return shownContext(tab.active)
  }
  const workspace = requireWorkspace(id)
  store.selectWorkspace(workspace.id)
  return shownContext(activeTab(workspace).active)
}

export const renameElement = (values: McpArguments): { kind: string; id: string; name: string; message: string } => {
  const name = textArgument(values, 'name')
  if (!name) {
    throw new Error('Indiquez le nouveau nom.')
  }
  const [kind, id] = singleTarget(values, ['workspace', 'tab'], 'Indiquez un seul élément à renommer : workspace ou tab.')
  const store = useSessionStore.getState()
  if (kind === 'workspace') {
    const workspace = requireWorkspace(id)
    store.renameWorkspace(workspace.id, name)
    return { kind, id, name, message: `Workspace « ${workspace.name} » renommé en « ${name} ».` }
  }
  const { tab } = requireTab(id)
  store.renameTab(tab.id, name)
  return { kind, id, name, message: `Onglet « ${tab.name} » renommé en « ${name} ».` }
}
