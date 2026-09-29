import type { FileEntry } from '../bridge/messages'
import type { DirectoryListing, EntryDraft } from '../store/explorerStore'

export enum RowKind {
  Entry = 'entry',
  Draft = 'draft',
  Note = 'note',
}

export interface TreeRow {
  kind: RowKind
  key: string
  depth: number
  parent: string
  entry?: FileEntry
  note?: string
}

export interface FileTreeView {
  rows: TreeRow[]
  watched: string[]
}

const LOADING_NOTE = 'Chargement…'

const hiddenNote = (listing: DirectoryListing): string | undefined => {
  const hidden = listing.total - listing.entries.length
  return hidden > 0 ? `… et ${hidden} autres éléments non affichés` : undefined
}

export const buildFileTree = (root: string, listings: Record<string, DirectoryListing>, expanded: Record<string, boolean>, draft: EntryDraft | null): FileTreeView => {
  const rows: TreeRow[] = []
  const watched: string[] = []
  const note = (parent: string, depth: number, text: string) => rows.push({ kind: RowKind.Note, key: `${parent}\n${text}`, depth, parent, note: text })
  const visit = (folder: string, depth: number) => {
    watched.push(folder)
    if (draft?.parent === folder) {
      rows.push({ kind: RowKind.Draft, key: `${folder}\ndraft`, depth, parent: folder })
    }
    const listing = listings[folder]
    if (!listing) {
      note(folder, depth, LOADING_NOTE)
      return
    }
    if (listing.error) {
      note(folder, depth, listing.error)
      return
    }
    for (const entry of listing.entries) {
      rows.push({ kind: RowKind.Entry, key: entry.path, depth, parent: folder, entry })
      if (entry.isDirectory && expanded[entry.path]) {
        visit(entry.path, depth + 1)
      }
    }
    const hidden = hiddenNote(listing)
    if (hidden) {
      note(folder, depth, hidden)
    }
  }
  visit(root, 0)
  return { rows, watched }
}

export const entryRows = (rows: TreeRow[]): TreeRow[] => rows.filter((row) => row.kind === RowKind.Entry)

const EXTENSION_SEPARATOR = '.'
const TRAILING_SEPARATORS = /[\\/]+$/
const PATH_SEPARATOR = '\\'

export const relativeEntryPath = (root: string, path: string): string => {
  const prefix = `${root.replace(TRAILING_SEPARATORS, '')}${PATH_SEPARATOR}`
  return path.toLowerCase().startsWith(prefix.toLowerCase()) ? path.slice(prefix.length) : path
}

export const renameSelectionEnd = (entry: FileEntry): number => {
  const separator = entry.name.lastIndexOf(EXTENSION_SEPARATOR)
  return entry.isDirectory || separator <= 0 ? entry.name.length : separator
}

export const targetFolder = (rows: TreeRow[], selectedPath: string | null, root: string): string => {
  const row = rows.find((candidate) => candidate.entry?.path === selectedPath)
  if (!row?.entry) {
    return root
  }
  return row.entry.isDirectory ? row.entry.path : row.parent
}
