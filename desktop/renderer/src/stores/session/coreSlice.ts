import { clampGitGraph, DEFAULT_GIT_GRAPH, EXPLORER_DEFAULT } from '../../model/session'
import { mutateSession } from './helpers'
import type { SessionSlice, CoreSlice } from './types'

export const createCoreSlice: SessionSlice<CoreSlice> = (set) => ({
  session: null,

  load: (session) =>
    set({ session: { ...session, closed: session.closed ?? [], favorites: session.favorites ?? [], explorerWidth: session.explorerWidth ?? EXPLORER_DEFAULT, gitGraph: clampGitGraph({ ...DEFAULT_GIT_GRAPH, ...session.gitGraph }) } }),

  change: (mutate) => set((state) => ({ session: mutateSession(state.session, mutate) })),
})
