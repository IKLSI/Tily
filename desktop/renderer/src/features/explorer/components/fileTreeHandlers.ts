import type { FileEntry } from '../../../bridge/messages'

export interface FileMenuRequest {
  x: number
  y: number
  entry?: FileEntry
  parent: string
}

export interface FileTreeHandlers {
  select: (path: string) => void
  activate: (entry: FileEntry) => void
  toggle: (path: string) => void
  openMenu: (request: FileMenuRequest) => void
  commitRename: (entry: FileEntry, parent: string, name: string) => void
  cancelRename: () => void
}

const ROW_BASE_PADDING_PX = 8
const ROW_INDENT_PX = 12
const ROW_CHEVRON_PX = 17

export const rowIndent = (depth: number): number => ROW_BASE_PADDING_PX + depth * ROW_INDENT_PX

export const leafIndent = (depth: number): number => rowIndent(depth) + ROW_CHEVRON_PX
