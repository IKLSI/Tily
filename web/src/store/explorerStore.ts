import { create } from 'zustand'
import type { EntryKind, FileEntry, GitPathMark } from '../bridge/messages'
import { folderMarksOf, markKey } from '../explorer/gitMarks'

export interface DirectoryListing {
  entries: FileEntry[]
  total: number
  error: string | null
}

export interface EntryDraft {
  parent: string
  kind: EntryKind
}

export interface DeleteRequest {
  path: string
  parent: string
  name: string
  isDirectory: boolean
}

interface ExplorerState {
  listings: Record<string, DirectoryListing>
  expanded: Record<string, boolean>
  selectedPath: string | null
  renamingPath: string | null
  draft: EntryDraft | null
  deleteRequest: DeleteRequest | null
  gitMarks: Map<string, GitPathMark>
  gitFolderMarks: Map<string, GitPathMark>
  setGitMarks: (root: string | null, marks: GitPathMark[]) => void
  setListing: (path: string, listing: DirectoryListing) => void
  setExpanded: (path: string, expanded: boolean) => void
  select: (path: string | null) => void
  startRename: (path: string) => void
  stopRename: () => void
  startDraft: (draft: EntryDraft) => void
  stopDraft: () => void
  requestDelete: (request: DeleteRequest) => void
  cancelDelete: () => void
  moved: (path: string, target: string) => void
  removed: (path: string) => void
}

const isWithin = (path: string, ancestor: string): boolean => path === ancestor || path.startsWith(`${ancestor.replace(/[\\/]+$/, '')}\\`)

export const useExplorerStore = create<ExplorerState>()((set) => ({
  listings: {},
  expanded: {},
  selectedPath: null,
  renamingPath: null,
  draft: null,
  deleteRequest: null,
  gitMarks: new Map(),
  gitFolderMarks: new Map(),
  setGitMarks: (root, marks) => set({ gitMarks: new Map(marks.map((mark) => [markKey(mark.path), mark])), gitFolderMarks: folderMarksOf(root, marks) }),
  setListing: (path, listing) => set((state) => ({ listings: { ...state.listings, [path]: listing } })),
  setExpanded: (path, expanded) => set((state) => (Boolean(state.expanded[path]) === expanded ? state : { expanded: { ...state.expanded, [path]: expanded } })),
  select: (selectedPath) => set({ selectedPath }),
  startRename: (renamingPath) => set({ renamingPath, draft: null }),
  stopRename: () => set({ renamingPath: null }),
  startDraft: (draft) => set((state) => ({ draft, renamingPath: null, expanded: { ...state.expanded, [draft.parent]: true } })),
  stopDraft: () => set({ draft: null }),
  requestDelete: (deleteRequest) => set({ deleteRequest }),
  cancelDelete: () => set({ deleteRequest: null }),
  moved: (path, target) =>
    set((state) => ({
      selectedPath: state.selectedPath === path ? target : state.selectedPath,
      expanded: state.expanded[path] ? { ...state.expanded, [path]: false, [target]: true } : state.expanded,
    })),
  removed: (path) => set((state) => ({ selectedPath: state.selectedPath && isWithin(state.selectedPath, path) ? null : state.selectedPath })),
}))
