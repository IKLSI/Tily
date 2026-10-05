import { create } from 'zustand'

export interface McpConsentRequest {
  id: number
  agent: string
  action: string
  target: string
  detail?: string
}

interface McpConsentState {
  queue: McpConsentRequest[]
  push: (request: McpConsentRequest) => void
  remove: (id: number) => void
}

export const useMcpConsentStore = create<McpConsentState>()((set) => ({
  queue: [],
  push: (request) => set((state) => ({ queue: [...state.queue, request] })),
  remove: (id) => set((state) => ({ queue: state.queue.filter((request) => request.id !== id) })),
}))
