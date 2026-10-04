import { panesOf, type Pane, type Session, type Tab, type Workspace } from '../model/session'
import { useSessionStore } from '../store/sessionStore'
import { terminalRegistry, type TerminalHandle } from '../terminal/terminalRegistry'

export interface McpPaneTarget {
  workspace: Workspace
  tab: Tab
  pane: Pane
  handle: TerminalHandle
}

interface PanePlace {
  workspace: Workspace
  tab: Tab
  pane: Pane
}

export const requireSession = (): Session => {
  const { session } = useSessionStore.getState()
  if (!session) {
    throw new Error('La session de Tily n’est pas encore chargée.')
  }
  return session
}

const placesOf = (session: Session): PanePlace[] =>
  session.workspaces.flatMap((workspace) => workspace.tabs.flatMap((tab) => panesOf(tab.tree).map((pane) => ({ workspace, tab, pane }))))

export const locationOf = (target: { workspace: Workspace; tab: Tab }): string => `${target.workspace.name} › ${target.tab.name}`

export const startedPanes = (session: Session): McpPaneTarget[] =>
  placesOf(session).flatMap((place) => {
    const handle = terminalRegistry.get(place.pane.id)
    return handle?.started ? [{ ...place, handle }] : []
  })

export const requireStartedPane = (paneId: string | undefined): McpPaneTarget => {
  if (!paneId) {
    throw new Error('Indiquez le pane visé : son identifiant est donné par tily_layout.')
  }
  const place = placesOf(requireSession()).find((candidate) => candidate.pane.id === paneId)
  if (!place) {
    throw new Error(`Pane inconnu : ${paneId}. Les identifiants des panes sont donnés par tily_layout.`)
  }
  const handle = terminalRegistry.get(paneId)
  if (!handle?.started) {
    throw new Error(`Le pane ${paneId} (${locationOf(place)}) n’a pas démarré : il n’a jamais été affiché depuis le lancement de Tily, son shell ne tourne pas.`)
  }
  return { ...place, handle }
}
