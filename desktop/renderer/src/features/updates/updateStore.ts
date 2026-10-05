import { create } from 'zustand'
import type { UpdateInfo } from '../../bridge/updateMessages'

interface UpdateState {
  info: UpdateInfo | null
  dialogOpen: boolean
  setInfo: (info: UpdateInfo) => void
  openDialog: () => void
  closeDialog: () => void
}

export const useUpdateStore = create<UpdateState>()((set) => ({
  info: null,
  dialogOpen: false,
  setInfo: (info) => set({ info }),
  openDialog: () => set({ dialogOpen: true }),
  closeDialog: () => set({ dialogOpen: false }),
}))
