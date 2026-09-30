import { current } from 'immer'
import { panesOf, pruneNode, splitLeaf, SplitAxis, tabNameFor, tabOfPane, type Session, type SplitNode, type Tab } from '../model/session'

const keepRemaining = (source: Tab, paneId: string, remaining: SplitNode): void => {
  source.tree = remaining
  if (source.active === paneId) {
    const next = panesOf(remaining)[0]
    source.active = next.id
    source.name = tabNameFor(source, next.id, next.path)
  }
}

export const movePaneOut = (draft: Session, paneId: string): void => {
  const workspace = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId)))
  const index = workspace ? workspace.tabs.findIndex((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId)) : -1
  const source = workspace?.tabs[index]
  const remaining = source ? pruneNode(source.tree, paneId) : null
  const pane = source ? panesOf(source.tree).find((candidate) => candidate.id === paneId) : undefined
  if (!workspace || !source || !remaining || !pane) {
    return
  }
  const tab = tabOfPane(current(pane))
  keepRemaining(source, paneId, remaining)
  workspace.tabs.splice(index + 1, 0, tab)
  workspace.active = tab.id
  draft.active = workspace.id
}

export const movePaneInto = (draft: Session, paneId: string, targetTabId: string): void => {
  const workspace = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => tab.id === targetTabId))
  const sourceWorkspace = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId)))
  const source = sourceWorkspace?.tabs.find((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId))
  const target = workspace?.tabs.find((tab) => tab.id === targetTabId)
  const pane = source ? panesOf(source.tree).find((candidate) => candidate.id === paneId) : undefined
  const remaining = source ? pruneNode(source.tree, paneId) : null
  if (!workspace || !sourceWorkspace || !source || !target || !pane || source.id === target.id || (!remaining && sourceWorkspace.tabs.length === 1)) {
    return
  }
  const moved = current(pane)
  if (remaining) {
    keepRemaining(source, paneId, remaining)
  } else {
    const index = sourceWorkspace.tabs.findIndex((tab) => tab.id === source.id)
    sourceWorkspace.tabs = sourceWorkspace.tabs.filter((tab) => tab.id !== source.id)
    if (sourceWorkspace.active === source.id) {
      sourceWorkspace.active = sourceWorkspace.tabs[Math.min(index, sourceWorkspace.tabs.length - 1)].id
    }
  }
  target.tree = splitLeaf(target.tree, target.active, SplitAxis.Horizontal, moved)
  target.active = moved.id
  workspace.active = target.id
  draft.active = workspace.id
}
