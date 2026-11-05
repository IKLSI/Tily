import { create } from 'zustand'
import { createCoreSlice } from './session/coreSlice'
import { createFavoritesSlice } from './session/favoritesSlice'
import { createLayoutSlice } from './session/layoutSlice'
import { createPaneSlice } from './session/paneSlice'
import { createSelectionSlice } from './session/selectionSlice'
import { createTabSlice } from './session/tabSlice'
import { createWorkspaceSlice } from './session/workspaceSlice'
import type { SessionState } from './session/types'

export const useSessionStore = create<SessionState>()((...args) => ({
  ...createCoreSlice(...args),
  ...createSelectionSlice(...args),
  ...createLayoutSlice(...args),
  ...createWorkspaceSlice(...args),
  ...createTabSlice(...args),
  ...createPaneSlice(...args),
  ...createFavoritesSlice(...args),
}))
