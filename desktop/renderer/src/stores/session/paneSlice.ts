import { insertPaneBesideIn, setBrowserPageIn, setPaneViewportIn } from '../browserPanes'
import { movePaneInto, movePaneOut } from '../paneMoves'
import { activePane, createPane, tabNameFor, firstPane, panesOf, pruneNode, setRatioAt, equalizeNode, swapPanes, splitLeaf, updatePane } from '../../model/session'
import { mutateSession, mutateTab, pathChanges } from './helpers'
import type { SessionSlice, PaneSlice } from './types'

export const createPaneSlice: SessionSlice<PaneSlice> = (set, get) => ({
  splitPane: (axis) =>
    set((state) => ({
      session: mutateTab(state.session, (tab) => {
        const current = activePane(tab)
        const fresh = createPane(current.path, current.shell)
        tab.tree = splitLeaf(tab.tree, current.id, axis, fresh)
        tab.active = fresh.id
      }),
    })),

  insertPaneBeside: (paneId, axis, pane) => set((state) => ({ session: mutateSession(state.session, (draft) => insertPaneBesideIn(draft, paneId, axis, pane)) })),

  setBrowserPage: (paneId, url, title) => set((state) => ({ session: mutateSession(state.session, (draft) => setBrowserPageIn(draft, paneId, url, title)) })),

  setPaneViewport: (paneId, viewport) => set((state) => ({ session: mutateSession(state.session, (draft) => setPaneViewportIn(draft, paneId, viewport)) })),

  setSplitRatio: (tabId, path, ratio) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const tab = draft.workspaces.flatMap((workspace) => workspace.tabs).find((candidate) => candidate.id === tabId)
        if (tab) {
          tab.tree = setRatioAt(tab.tree, path, ratio)
        }
      }),
    })),

  equalizeSplits: (tabId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const tab = draft.workspaces.flatMap((workspace) => workspace.tabs).find((candidate) => candidate.id === tabId)
        if (tab) {
          tab.tree = equalizeNode(tab.tree)
        }
      }),
    })),

  swapActivePane: (targetPaneId) =>
    set((state) => ({
      session: mutateTab(state.session, (tab) => {
        tab.tree = swapPanes(tab.tree, tab.active, targetPaneId)
      }),
    })),

  movePaneToNewTab: (paneId) => set((state) => ({ session: mutateSession(state.session, (draft) => movePaneOut(draft, paneId)) })),

  movePaneToTab: (paneId, targetTabId) => set((state) => ({ session: mutateSession(state.session, (draft) => movePaneInto(draft, paneId, targetTabId)) })),

  closePane: (paneId) => {
    const session = get().session
    if (!session) {
      return
    }
    const workspace = session.workspaces.find((candidate) => candidate.tabs.some((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId)))
    const tab = workspace?.tabs.find((candidate) => panesOf(candidate.tree).some((pane) => pane.id === paneId))
    if (!workspace || !tab) {
      return
    }
    if (panesOf(tab.tree).length === 1) {
      get().closeTab(tab.id)
      return
    }
    set({
      session: mutateSession(session, (draft) => {
        const draftTab = draft.workspaces.find((candidate) => candidate.id === workspace.id)!.tabs.find((candidate) => candidate.id === tab.id)!
        draftTab.tree = pruneNode(draftTab.tree, paneId) ?? draftTab.tree
        if (draftTab.active === paneId) {
          draftTab.active = firstPane(draftTab.tree).id
        }
      }),
    })
  },

  setPaneShell: (paneId, shell) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        for (const workspace of draft.workspaces) {
          for (const tab of workspace.tabs) {
            tab.tree = updatePane(tab.tree, paneId, { shell })
          }
        }
      }),
    })),

  setPanePath: (paneId, path) =>
    set((state) =>
      pathChanges(state.session, paneId, path)
        ? {
            session: mutateSession(state.session, (draft) => {
              for (const workspace of draft.workspaces) {
                for (const tab of workspace.tabs) {
                  tab.tree = updatePane(tab.tree, paneId, { path })
                  tab.name = tabNameFor(tab, paneId, path)
                }
              }
            }),
          }
        : state,
    ),
})
