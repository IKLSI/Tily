import { panesOf } from '../../model/session'
import { mutateSession, mutateWorkspace } from './helpers'
import type { SessionSlice, SelectionSlice } from './types'

export const createSelectionSlice: SessionSlice<SelectionSlice> = (set) => ({
  selectWorkspace: (workspaceId) =>
    set((state) => ({ session: mutateSession(state.session, (draft) => { draft.active = workspaceId }) })),

  selectTab: (tabId) =>
    set((state) => ({ session: mutateWorkspace(state.session, (workspace) => { workspace.active = tabId }) })),

  selectPane: (paneId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        for (const workspace of draft.workspaces) {
          for (const tab of workspace.tabs) {
            if (panesOf(tab.tree).some((pane) => pane.id === paneId)) {
              draft.active = workspace.id
              workspace.active = tab.id
              tab.active = paneId
            }
          }
        }
      }),
    })),
})
