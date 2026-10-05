import { distinctWorkspaceName, type Session } from '../../../model/session'
import { useSessionStore } from '../../../stores/sessionStore'
import { copyPanePath } from '../../terminal/contextActions'
import type { ActionMenuItem } from '../../../components/ActionMenu'

const copyTabPath = (session: Session | null, tabId: string): void => {
  const paneId = session?.workspaces.flatMap((workspace) => workspace.tabs).find((tab) => tab.id === tabId)?.active
  if (paneId) {
    copyPanePath(paneId)
  }
}

export const tabTransferItems = (session: Session | null, tabId: string): ActionMenuItem[] => {
  const workspaces = session?.workspaces ?? []
  const source = workspaces.find((workspace) => workspace.tabs.some((tab) => tab.id === tabId))
  return [
    ...workspaces
      .filter((workspace) => workspace !== source)
      .map((workspace) => ({ id: `move-to-${workspace.id}`, label: `Déplacer vers « ${distinctWorkspaceName(workspaces, workspace)} »`, run: () => useSessionStore.getState().moveTab(tabId, workspace.id) })),
    { id: 'copy-path', label: 'Copier le chemin', run: () => copyTabPath(session, tabId) },
  ]
}
