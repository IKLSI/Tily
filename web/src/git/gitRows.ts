import { GitDiffSource, type GitConflict, type GitFileChange } from '../bridge/gitMessages'
import { useGitStore } from '../store/gitStore'
import { openInEditor, showChange } from './gitRequests'

export enum GitRowGroup {
  Conflict = 'conflict',
  Staged = 'staged',
  Unstaged = 'unstaged',
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
  focus: (key: string) => void
}

export const rowKey = (group: GitRowGroup, path: string): string => `${group}\n${path}`

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
