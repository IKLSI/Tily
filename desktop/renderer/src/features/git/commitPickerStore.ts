import { create } from 'zustand'

interface CommitPickerState {
  open: boolean
  setOpen: (open: boolean) => void
}

export const useCommitPickerStore = create<CommitPickerState>()((set) => ({
  open: false,
  setOpen: (open) => set({ open }),
}))

export const commitPickerOpen = (): boolean => useCommitPickerStore.getState().open
