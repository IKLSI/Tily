import type { StateCreator } from 'zustand'
import type { BrowserViewport } from '../../model/browser'
import type { GitGraphLayout, Pane, RightPanelView, Session, SplitAxis, SplitPath, Tab } from '../../model/session'

export interface CoreSlice {
  session: Session | null
  load: (session: Session) => void
  change: (mutate: (draft: Session) => void) => void
}

export interface SelectionSlice {
  selectWorkspace: (workspaceId: string) => void
  selectTab: (tabId: string) => void
  selectPane: (paneId: string) => void
}

export interface LayoutSlice {
  toggleSidebar: () => void
  setSidebarWidth: (width: number) => void
  toggleExplorer: () => boolean
  togglePanelView: (view: RightPanelView) => boolean
  setPanelView: (view: RightPanelView) => void
  setExplorerWidth: (width: number) => void
  setGitGraphLayout: (change: Partial<GitGraphLayout>) => void
}

export interface WorkspaceSlice {
  toggleWorkspace: (workspaceId: string) => void
  collapseOtherWorkspaces: (workspaceId: string) => void
  newWorkspace: (name: string, path: string, shell: string) => string
  renameWorkspace: (workspaceId: string, name: string) => void
  setWorkspaceNote: (workspaceId: string, note: string) => void
  setWorkspaceColor: (workspaceId: string, color: string | undefined) => void
  moveWorkspace: (workspaceId: string, offset: number) => void
  moveWorkspaceBefore: (workspaceId: string, beforeWorkspaceId?: string) => void
}

export interface TabSlice {
  newTab: (shell: string) => void
  newTabAt: (path: string, shell: string) => void
  renameTab: (tabId: string, name: string) => void
  resetTabName: (tabId: string) => void
  setTabColor: (tabId: string, color: string | undefined) => void
  moveTab: (tabId: string, targetWorkspaceId: string, beforeTabId?: string) => void
  moveActiveTab: (offset: number) => void
  shiftTab: (tabId: string, offset: number) => void
  duplicateTab: (tabId: string) => void
  selectAdjacentTab: (offset: number) => void
  closeTab: (tabId: string) => void
  restoreTab: (position?: number) => { tab: Tab; paneIds: Record<string, string> } | null
  newTabWithPane: (pane: Pane) => void
}

export interface PaneSlice {
  splitPane: (axis: SplitAxis) => void
  insertPaneBeside: (paneId: string, axis: SplitAxis, pane: Pane) => void
  setBrowserPage: (paneId: string, url: string, title: string) => void
  setPaneViewport: (paneId: string, viewport: BrowserViewport) => void
  setSplitRatio: (tabId: string, path: SplitPath, ratio: number) => void
  equalizeSplits: (tabId: string) => void
  swapActivePane: (targetPaneId: string) => void
  movePaneToNewTab: (paneId: string) => void
  movePaneToTab: (paneId: string, targetTabId: string) => void
  closePane: (paneId: string) => void
  setPanePath: (paneId: string, path: string) => void
  setPaneShell: (paneId: string, shell: string) => void
}

export interface FavoritesSlice {
  toggleFavorite: (commandId: string) => void
  setFavorites: (commandIds: string[]) => void
}

export type SessionState = CoreSlice & SelectionSlice & LayoutSlice & WorkspaceSlice & TabSlice & PaneSlice & FavoritesSlice

export type SessionSlice<Slice> = StateCreator<SessionState, [], [], Slice>
