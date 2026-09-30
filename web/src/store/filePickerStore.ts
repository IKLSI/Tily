import { create } from 'zustand'

export interface ProjectFileList {
  root: string
  files: string[]
  truncated: boolean
  error: string | null
}

interface FilePickerState {
  folder: string | null
  list: ProjectFileList | null
  open: (folder: string) => void
  receive: (folder: string, list: ProjectFileList) => void
  close: () => void
}

export const useFilePickerStore = create<FilePickerState>()((set, get) => ({
  folder: null,
  list: null,
  open: (folder) => set({ folder, list: null }),
  receive: (folder, list) => {
    if (get().folder === folder) {
      set({ list })
    }
  },
  close: () => set({ folder: null, list: null }),
}))

export const filePickerOpen = (): boolean => useFilePickerStore.getState().folder !== null
