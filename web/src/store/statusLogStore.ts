import { create } from 'zustand'
import type { StatusLogEntry } from '../bridge/statusLogMessages'

export const STATUS_LOG_MAX_ENTRIES = 500

interface StatusLogState {
  entries: StatusLogEntry[]
  open: boolean
  load: (entries: StatusLogEntry[]) => void
  add: (entry: StatusLogEntry) => void
  clear: () => void
  setOpen: (open: boolean) => void
}

export const useStatusLogStore = create<StatusLogState>()((set) => ({
  entries: [],
  open: false,
  load: (entries) => set({ entries: entries.slice(-STATUS_LOG_MAX_ENTRIES) }),
  add: (entry) => set((state) => ({ entries: [...state.entries, entry].slice(-STATUS_LOG_MAX_ENTRIES) })),
  clear: () => set({ entries: [] }),
  setOpen: (open) => set({ open }),
}))
