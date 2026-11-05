import { current } from 'immer'
import { activePane, activeTab, CLOSED_TABS_MAX, cloneTabWithNewIds, createTab, disownTab, findWorkspace, mergedNote, tabAt, paneName, tabOfPane, type ClosedTab } from '../../model/session'
import { mutateSession, mutateWorkspace } from './helpers'
import type { SessionSlice, TabSlice } from './types'

export const createTabSlice: SessionSlice<TabSlice> = (set, get) => ({
  newTab: (shell) =>
    set((state) => ({
      session: mutateWorkspace(state.session, (workspace) => {
        const tab = createTab(activePane(activeTab(workspace)).path, shell)
        workspace.tabs.push(tab)
        workspace.active = tab.id
      }),
    })),

  newTabAt: (path, shell) =>
    set((state) => ({
      session: mutateWorkspace(state.session, (workspace) => {
        const tab = createTab(path, shell)
        workspace.tabs.push(tab)
        workspace.active = tab.id
      }),
    })),

  renameTab: (tabId, name) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const tab = draft.workspaces.flatMap((workspace) => workspace.tabs).find((candidate) => candidate.id === tabId)
        const trimmed = name.trim()
        if (tab && trimmed.length > 0) {
          tab.name = trimmed
          tab.manual = true
        }
      }),
    })),

  resetTabName: (tabId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const tab = draft.workspaces.flatMap((workspace) => workspace.tabs).find((candidate) => candidate.id === tabId)
        if (tab) {
          tab.manual = false
          tab.name = paneName(activePane(tab)) || tab.name
        }
      }),
    })),

  setTabColor: (tabId, color) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const tab = draft.workspaces.flatMap((workspace) => workspace.tabs).find((candidate) => candidate.id === tabId)
        if (tab) {
          tab.color = color
        }
      }),
    })),

  moveTab: (tabId, targetWorkspaceId, beforeTabId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const source = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => tab.id === tabId))
        const target = findWorkspace(draft, targetWorkspaceId)
        if (!source || !target || tabId === beforeTabId) {
          return
        }
        const sourceIndex = source.tabs.findIndex((candidate) => candidate.id === tabId)
        const tab = tabAt(source, sourceIndex)
        source.tabs.splice(sourceIndex, 1)
        if (source.tabs.length === 0 && source !== target) {
          if (source.note) {
            target.note = mergedNote(target.note, source.name, source.note)
          }
          draft.workspaces = draft.workspaces.filter((candidate) => candidate !== source)
        } else if (source.active === tabId && source !== target) {
          source.active = tabAt(source, 0).id
        }
        const index = beforeTabId ? target.tabs.findIndex((candidate) => candidate.id === beforeTabId) : -1
        target.tabs.splice(index < 0 ? target.tabs.length : index, 0, tab)
        target.active = tab.id
        draft.active = target.id
      }),
    })),

  moveActiveTab: (offset) =>
    set((state) => ({
      session: mutateWorkspace(state.session, (workspace) => {
        const index = workspace.tabs.findIndex((tab) => tab.id === workspace.active)
        const destination = index + offset
        if (destination < 0 || destination >= workspace.tabs.length) {
          return
        }
        workspace.tabs.splice(destination, 0, ...workspace.tabs.splice(index, 1))
      }),
    })),

  shiftTab: (tabId, offset) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => tab.id === tabId))
        const index = workspace ? workspace.tabs.findIndex((tab) => tab.id === tabId) : -1
        const destination = index + offset
        if (!workspace || index < 0 || destination < 0 || destination >= workspace.tabs.length) {
          return
        }
        workspace.tabs.splice(destination, 0, ...workspace.tabs.splice(index, 1))
      }),
    })),

  duplicateTab: (tabId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => tab.id === tabId))
        const index = workspace ? workspace.tabs.findIndex((tab) => tab.id === tabId) : -1
        if (!workspace || index < 0) {
          return
        }
        const { tab } = cloneTabWithNewIds(disownTab(current(tabAt(workspace, index))))
        workspace.tabs.splice(index + 1, 0, tab)
        workspace.active = tab.id
        draft.active = workspace.id
      }),
    })),

  selectAdjacentTab: (offset) =>
    set((state) => ({
      session: mutateWorkspace(state.session, (workspace) => {
        const count = workspace.tabs.length
        const index = Math.max(0, workspace.tabs.findIndex((tab) => tab.id === workspace.active))
        workspace.active = tabAt(workspace, (((index + offset) % count) + count) % count).id
      }),
    })),

  closeTab: (tabId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = draft.workspaces.find((candidate) => candidate.tabs.some((tab) => tab.id === tabId))
        if (!workspace) {
          return
        }
        const index = workspace.tabs.findIndex((tab) => tab.id === tabId)
        const tab = tabAt(workspace, index)
        workspace.tabs.splice(index, 1)
        const closed: ClosedTab = { workspaceId: workspace.id, workspaceName: workspace.name, index, tab }
        if (workspace.tabs.length === 0 && workspace.note) {
          closed.workspaceNote = workspace.note
        }
        draft.closed = [...draft.closed, closed].slice(-CLOSED_TABS_MAX)
        if (workspace.tabs.length === 0) {
          draft.workspaces = draft.workspaces.filter((candidate) => candidate.id !== workspace.id)
          if (draft.active === workspace.id) {
            draft.active = draft.workspaces[0]?.id ?? ''
          }
          return
        }
        if (workspace.active === tabId) {
          workspace.active = tabAt(workspace, Math.min(index, workspace.tabs.length - 1)).id
        }
      }),
    })),

  restoreTab: (position) => {
    const closed = get().session?.closed ?? []
    const index = position ?? closed.length - 1
    const entry = closed[index]
    if (!entry) {
      return null
    }
    const { tab, paneIds } = cloneTabWithNewIds(entry.tab)
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        draft.closed = draft.closed.filter((_, candidate) => candidate !== index)
        let workspace = findWorkspace(draft, entry.workspaceId)
        if (!workspace) {
          workspace = { id: entry.workspaceId, name: entry.workspaceName, tabs: [], active: tab.id, expanded: true }
          draft.workspaces.push(workspace)
        }
        if (entry.workspaceNote && !workspace.note) {
          workspace.note = entry.workspaceNote
        }
        workspace.tabs.splice(Math.min(entry.index, workspace.tabs.length), 0, tab)
        workspace.active = tab.id
        draft.active = workspace.id
      }),
    }))
    return { tab, paneIds }
  },

  newTabWithPane: (pane) =>
    set((state) => ({
      session: mutateWorkspace(state.session, (workspace) => {
        const tab = tabOfPane(pane)
        workspace.tabs.push(tab)
        workspace.active = tab.id
      }),
    })),
})
