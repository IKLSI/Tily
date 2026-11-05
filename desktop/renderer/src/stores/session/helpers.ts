import { produce } from 'immer'
import { activeTab, activeWorkspace, tabNameFor, panesOf, type Session, type Tab, type Workspace } from '../../model/session'

export const mutateSession = (session: Session | null, mutate: (draft: Session) => void): Session | null => (session ? produce(session, mutate) : session)

export const mutateWorkspace = (session: Session | null, mutate: (workspace: Workspace, draft: Session) => void): Session | null =>
  mutateSession(session, (draft) => {
    const workspace = activeWorkspace(draft)
    if (workspace) {
      mutate(workspace, draft)
    }
  })

export const mutateTab = (session: Session | null, mutate: (tab: Tab, workspace: Workspace, draft: Session) => void): Session | null =>
  mutateWorkspace(session, (workspace, draft) => mutate(activeTab(workspace), workspace, draft))

export const rightPanelOpen = (session: Session | null): boolean => {
  const workspace = session ? activeWorkspace(session) : undefined
  return Boolean(workspace && activeTab(workspace).explorer)
}

export const pathChanges = (session: Session | null, paneId: string, path: string): boolean =>
  (session?.workspaces ?? []).some((workspace) =>
    workspace.tabs.some((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId && (pane.path !== path || tabNameFor(tab, paneId, path) !== tab.name))),
  )
