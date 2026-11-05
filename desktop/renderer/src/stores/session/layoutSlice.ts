import { clampGitGraph, EXPLORER_MAX, EXPLORER_MIN, RightPanelView, SIDEBAR_MAX, SIDEBAR_MIN } from '../../model/session'
import { mutateSession, mutateTab, rightPanelOpen } from './helpers'
import type { SessionSlice, LayoutSlice } from './types'

export const createLayoutSlice: SessionSlice<LayoutSlice> = (set, get) => ({
  toggleSidebar: () =>
    set((state) => ({ session: mutateSession(state.session, (draft) => { draft.sidebarCollapsed = !draft.sidebarCollapsed }) })),

  setSidebarWidth: (width) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => { draft.sidebar = Math.min(SIDEBAR_MAX, Math.max(SIDEBAR_MIN, Math.round(width))) }),
    })),

  toggleExplorer: () => {
    set((state) => ({ session: mutateTab(state.session, (tab) => { tab.explorer = !tab.explorer }) }))
    return rightPanelOpen(get().session)
  },

  togglePanelView: (view) => {
    set((state) => ({
      session: mutateTab(state.session, (tab) => {
        const shown = tab.explorer && (tab.panel ?? RightPanelView.Files) === view
        tab.explorer = !shown
        tab.panel = view
      }),
    }))
    return rightPanelOpen(get().session)
  },

  setPanelView: (view) =>
    set((state) => ({ session: mutateTab(state.session, (tab) => { tab.panel = view }) })),

  setExplorerWidth: (width) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => { draft.explorerWidth = Math.min(EXPLORER_MAX, Math.max(EXPLORER_MIN, Math.round(width))) }),
    })),

  setGitGraphLayout: (change) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => { Object.assign(draft.gitGraph, clampGitGraph({ ...draft.gitGraph, ...change })) }),
    })),
})
