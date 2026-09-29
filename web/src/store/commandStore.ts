import { create } from 'zustand'

export interface CommandNotice {
  success: boolean
  durationMs: number
  command: string
}

interface CommandState {
  notices: Record<string, CommandNotice>
  notify: (paneId: string, notice: CommandNotice) => void
  keepOnly: (keep: (paneId: string) => boolean) => void
}

export const useCommandStore = create<CommandState>()((set) => ({
  notices: {},
  notify: (paneId, notice) => set((state) => ({ notices: { ...state.notices, [paneId]: notice } })),
  keepOnly: (keep) =>
    set((state) => {
      const kept = Object.entries(state.notices).filter(([paneId]) => keep(paneId))
      return kept.length === Object.keys(state.notices).length ? state : { notices: Object.fromEntries(kept) }
    }),
}))
