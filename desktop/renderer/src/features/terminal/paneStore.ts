import { create } from 'zustand'

export enum PaneStateKind {
  Failed = 'failed',
  Exited = 'exited',
  PathMissing = 'pathMissing',
}

export interface PaneState {
  kind: PaneStateKind
  message: string
  fallback?: string
}

interface PaneStoreState {
  states: Record<string, PaneState>
  devServers: Record<string, string[]>
  markFailed: (paneId: string, message: string) => void
  markExited: (paneId: string, code: number) => void
  markPathMissing: (paneId: string, path: string, fallback: string) => void
  markAlive: (paneId: string) => void
  dismiss: (paneId: string) => void
  clear: (paneId: string) => void
  showDevServer: (paneId: string, url: string) => void
  clearDevServer: (paneId: string) => void
}

const without = <T,>(states: Record<string, T>, paneId: string): Record<string, T> => {
  const { [paneId]: _removed, ...rest } = states
  return rest
}

const ignoredPaths = new Map<string, string>()

export const usePaneStore = create<PaneStoreState>()((set) => ({
  states: {},
  devServers: {},
  markFailed: (paneId, message) =>
    set((state) => (state.states[paneId]?.kind === PaneStateKind.Exited ? state : { states: { ...state.states, [paneId]: { kind: PaneStateKind.Failed, message } } })),
  markExited: (paneId, code) =>
    set((state) => ({ states: { ...state.states, [paneId]: { kind: PaneStateKind.Exited, message: `Le shell s’est terminé (code ${code}).` } } })),
  markPathMissing: (paneId, path, fallback) =>
    set((state) => {
      const blocked = (state.states[paneId] && state.states[paneId].kind !== PaneStateKind.PathMissing) || ignoredPaths.get(paneId) === path
      return blocked ? state : { states: { ...state.states, [paneId]: { kind: PaneStateKind.PathMissing, message: path, fallback } } }
    }),
  markAlive: (paneId) =>
    set((state) => (state.states[paneId] && state.states[paneId].kind !== PaneStateKind.PathMissing ? { states: without(state.states, paneId) } : state)),
  dismiss: (paneId) =>
    set((state) => {
      const current = state.states[paneId]
      if (current?.kind === PaneStateKind.PathMissing) {
        ignoredPaths.set(paneId, current.message)
      }
      return { states: without(state.states, paneId) }
    }),
  clear: (paneId) => set((state) => (state.states[paneId] ? { states: without(state.states, paneId) } : state)),
  showDevServer: (paneId, url) =>
    set((state) => (state.devServers[paneId]?.includes(url) ? state : { devServers: { ...state.devServers, [paneId]: [...(state.devServers[paneId] ?? []), url] } })),
  clearDevServer: (paneId) => set((state) => (state.devServers[paneId] ? { devServers: without(state.devServers, paneId) } : state)),
}))
