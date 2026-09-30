import { create } from 'zustand'

export interface ProjectFileList {
  root: string
  files: string[]
  changed: string[]
  truncated: boolean
  error: string | null
}

const MAX_RECENT = 10

export const recentKey = (root: string): string => root.toLowerCase()

interface FilePickerState {
  folder: string | null
  list: ProjectFileList | null
  recent: Record<string, string[]>
  open: (folder: string) => void
  remember: (root: string, relative: string) => void
  receive: (folder: string, list: ProjectFileList) => void
  close: () => void
}

export const useFilePickerStore = create<FilePickerState>()((set, get) => ({
  folder: null,
  list: null,
  recent: {},
  open: (folder) => set({ folder, list: null }),
  remember: (root, relative) => {
    const key = recentKey(root)
    const previous = get().recent[key] ?? []
    set({ recent: { ...get().recent, [key]: [relative, ...previous.filter((entry) => entry !== relative)].slice(0, MAX_RECENT) } })
  },
  receive: (folder, list) => {
    if (get().folder === folder) {
      set({ list })
    }
  },
  close: () => set({ folder: null, list: null }),
}))

export const filePickerOpen = (): boolean => useFilePickerStore.getState().folder !== null
