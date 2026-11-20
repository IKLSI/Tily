import { create } from 'zustand'
import { DEFAULT_SEARCH_OPTIONS, type SearchOption, type SearchOptions } from '../../model/textSearch'

interface TerminalSearchState {
  open: boolean
  query: string
  options: SearchOptions
  show: (query?: string) => void
  hide: () => void
  setQuery: (query: string) => void
  toggleOption: (option: SearchOption) => void
}

export const useTerminalSearchStore = create<TerminalSearchState>()((set, get) => ({
  open: false,
  query: '',
  options: DEFAULT_SEARCH_OPTIONS,
  show: (query) => set({ open: true, query: query ?? get().query }),
  hide: () => set({ open: false }),
  setQuery: (query) => set({ query }),
  toggleOption: (option) => set({ options: { ...get().options, [option]: !get().options[option] } }),
}))

export const terminalSearchOpen = (): boolean => useTerminalSearchStore.getState().open
