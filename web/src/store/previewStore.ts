import { create } from 'zustand'
import type { FilePreview } from '../bridge/previewMessages'

interface PreviewState {
  path: string | null
  preview: FilePreview | null
  anchor: string | null
  anchorRequest: number
  expanded: boolean
  deferred: Record<string, string>
  open: (path: string) => void
  receive: (preview: FilePreview, anchor: string | null, reload: boolean) => void
  jumpTo: (anchor: string) => void
  close: () => void
  toggleExpanded: () => void
  defer: (tabId: string, path: string) => void
  takeDeferred: (tabId: string) => string | null
}

export const usePreviewStore = create<PreviewState>((set, get) => ({
  path: null,
  preview: null,
  anchor: null,
  anchorRequest: 0,
  expanded: false,
  deferred: {},
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
  toggleExpanded: () => set((state) => ({ expanded: !state.expanded })),
  defer: (tabId, path) => set((state) => ({ deferred: { ...state.deferred, [tabId]: path } })),
  takeDeferred: (tabId) => {
    const { [tabId]: path, ...rest } = get().deferred
    if (path === undefined) {
      return null
    }
    set({ deferred: rest })
    return path
  },
}))
