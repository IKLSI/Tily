import type { WorktreeTarget } from '../worktree/worktreePaths'
import { requestWorktreeRemoval } from '../worktree/worktreeActions'
import { closeOtherTabsKeepingText, closeTabsToRightKeepingText } from '../terminal/tabLifecycle'
import type { ActionMenuItem } from './ActionMenu'
import { FloatingMenu } from './FloatingMenu'
import { MenuShortcut } from './MenuShortcut'
import type { MenuPlace, PanelMenuRequest, WorkspacePanelActions } from './workspacePanel'

interface WorkspaceContextMenuProps {
  request: PanelMenuRequest
  place: MenuPlace
  actions: WorkspacePanelActions
  worktrees: WorktreeTarget[]
  onRun: () => void
  onDismiss: () => void
}

const moveItems = ({ position, count }: MenuPlace, move: (offset: number) => void): ActionMenuItem[] => [
  { id: 'move-up', label: 'Monter', detail: <MenuShortcut keys="Alt + ↑" />, disabled: position <= 0, run: () => move(-1) },
  { id: 'move-down', label: 'Descendre', detail: <MenuShortcut keys="Alt + ↓" />, disabled: position < 0 || position >= count - 1, run: () => move(1) },
]

const worktreeItems = (worktrees: WorktreeTarget[]): ActionMenuItem[] =>
  worktrees.map(({ path, name, branch }) => ({ id: `remove-worktree-${path}`, label: `Supprimer le worktree « ${name} »…`, run: () => requestWorktreeRemoval(path, branch) }))

const itemsFor = ({ workspaceId, tabId }: PanelMenuRequest, place: MenuPlace, actions: WorkspacePanelActions): ActionMenuItem[] =>
  tabId
    ? [
        { id: 'rename-tab', label: 'Renommer', detail: <MenuShortcut keys="F2" />, run: () => actions.startRenameTab(tabId) },
        { id: 'duplicate-tab', label: 'Dupliquer l’onglet', run: () => actions.duplicateTab(tabId) },
        ...moveItems(place, (offset) => actions.shiftTab(tabId, offset)),
        { id: 'close-tab', label: 'Fermer l’onglet', run: () => actions.closeTab(tabId) },
        { id: 'close-other-tabs', label: 'Fermer les autres onglets', disabled: place.count <= 1, run: () => closeOtherTabsKeepingText(tabId) },
        { id: 'close-tabs-below', label: 'Fermer les onglets en dessous', disabled: place.position < 0 || place.position >= place.count - 1, run: () => closeTabsToRightKeepingText(tabId) },
      ]
    : [
        { id: 'rename-workspace', label: 'Renommer', detail: <MenuShortcut keys="F2" />, run: () => actions.startRenameWorkspace(workspaceId) },
        { id: 'new-tab', label: 'Nouvel onglet PowerShell', run: () => actions.newTabIn(workspaceId) },
        { id: 'open-notes', label: 'Notes du workspace', run: () => actions.openNotes(workspaceId) },
        ...moveItems(place, (offset) => actions.moveWorkspace(workspaceId, offset)),
        { id: 'collapse-others', label: 'Replier les autres', run: () => actions.collapseOthers(workspaceId) },
        { id: 'close-workspace', label: 'Fermer le workspace', run: () => actions.closeWorkspace(workspaceId) },
      ]

export function WorkspaceContextMenu({ request, place, actions, worktrees, onRun, onDismiss }: WorkspaceContextMenuProps) {
  const { x, y, tabId } = request

  const closingFirst = (item: ActionMenuItem): ActionMenuItem => ({
    ...item,
    run: () => {
      onRun()
      item.run()
    },
  })

  return <FloatingMenu x={x} y={y} label={tabId ? 'Actions de l’onglet' : 'Actions du workspace'} items={[...itemsFor(request, place, actions), ...worktreeItems(worktrees)].map(closingFirst)} onClose={onDismiss} />
}
