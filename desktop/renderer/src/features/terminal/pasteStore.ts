import { create } from 'zustand'

export interface PasteRequest {
  paneId: string
  text: string
  lines: string[]
}

interface PasteState {
  request: PasteRequest | null
  ask: (request: PasteRequest) => void
  clear: () => void
}

export const usePasteStore = create<PasteState>()((set) => ({
  request: null,
  ask: (request) => set({ request }),
  clear: () => set({ request: null }),
}))
