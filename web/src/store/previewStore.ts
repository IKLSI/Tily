import { create } from 'zustand'
import type { FilePreview } from '../bridge/previewMessages'

interface PreviewState {
  path: string | null
  preview: FilePreview | null
  anchor: string | null
  anchorRequest: number
  open: (path: string) => void
  receive: (preview: FilePreview, anchor: string | null, reload: boolean) => void
  jumpTo: (anchor: string) => void
  close: () => void
}

export const usePreviewStore = create<PreviewState>((set, get) => ({
  path: null,
  preview: null,
  anchor: null,
  anchorRequest: 0,
  open: (path) => set((state) => ({ path, preview: state.preview?.path === path ? state.preview : null })),
  receive: (preview, anchor, reload) => {
    const { path, anchorRequest } = get()
    if (path === null || (reload && preview.path !== path)) {
      return
    }
    set(reload ? { preview } : { path: preview.path, preview, anchor, anchorRequest: anchorRequest + 1 })
  },
  jumpTo: (anchor) => set((state) => ({ anchor, anchorRequest: state.anchorRequest + 1 })),
  close: () => set({ path: null, preview: null, anchor: null }),
}))
