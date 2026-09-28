import { GitDiffSource, type GitConflict, type GitFileChange } from '../bridge/gitMessages'
import { useGitStore } from '../store/gitStore'
import { openInEditor, showChange } from './gitRequests'

export enum GitRowGroup {
  Conflict = 'conflict',
  Staged = 'staged',
  Unstaged = 'unstaged',
}

export enum GitSelectMode {
  Replace = 'replace',
  Toggle = 'toggle',
  Range = 'range',
}

export interface GitChangeRow {
  key: string
  group: GitRowGroup
  change?: GitFileChange
  conflict?: GitConflict
}

export interface GitRowHandlers {
  open: (row: GitChangeRow) => void
  stage: (change: GitFileChange) => void
  unstage: (change: GitFileChange) => void
  discard: (change: GitFileChange) => void
  edit: (path: string) => void
  resolve: (conflict: GitConflict) => void
  select: (row: GitChangeRow, mode: GitSelectMode) => void
  menu: (row: GitChangeRow, x: number, y: number) => void
}

export const rowKey = (group: GitRowGroup, path: string): string => `${group}\n${path}`

export const rowPath = ({ change, conflict }: GitChangeRow): string => change?.path ?? conflict?.path ?? ''

export const selectModeOf = ({ ctrlKey, metaKey, shiftKey }: { ctrlKey: boolean; metaKey: boolean; shiftKey: boolean }): GitSelectMode =>
  shiftKey ? GitSelectMode.Range : ctrlKey || metaKey ? GitSelectMode.Toggle : GitSelectMode.Replace

export const nextSelection = (rows: GitChangeRow[], selection: ReadonlySet<string>, anchor: string | null, key: string, mode: GitSelectMode): Set<string> => {
  if (mode === GitSelectMode.Toggle) {
    const toggled = new Set(selection)
    if (!toggled.delete(key)) {
      toggled.add(key)
    }
    return toggled
  }
  const start = rows.findIndex((row) => row.key === anchor)
  const end = rows.findIndex((row) => row.key === key)
  if (mode === GitSelectMode.Replace || start < 0 || end < 0) {
    return new Set([key])
  }
  return new Set(rows.slice(Math.min(start, end), Math.max(start, end) + 1).map((row) => row.key))
}

export const changeRows = (conflicts: GitConflict[], staged: GitFileChange[], unstaged: GitFileChange[]): GitChangeRow[] => [
  ...conflicts.map((conflict) => ({ key: rowKey(GitRowGroup.Conflict, conflict.path), group: GitRowGroup.Conflict, conflict })),
  ...staged.map((change) => ({ key: rowKey(GitRowGroup.Staged, change.path), group: GitRowGroup.Staged, change })),
  ...unstaged.map((change) => ({ key: rowKey(GitRowGroup.Unstaged, change.path), group: GitRowGroup.Unstaged, change })),
]

export const openChangeRow = (row: GitChangeRow): void => {
  if (row.conflict) {
    openInEditor(row.conflict.path)
  } else if (row.change) {
    showChange(row.change, row.group === GitRowGroup.Staged ? GitDiffSource.Staged : GitDiffSource.Unstaged)
  }
}

export const drawerShowsWorkingFile = (): boolean => {
  const { file, commit } = useGitStore.getState()
  return Boolean(file && !commit)
}
