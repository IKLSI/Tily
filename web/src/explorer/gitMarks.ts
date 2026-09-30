import { GitChangeKind } from '../bridge/gitMessages'
import type { GitPathMark } from '../bridge/messages'
import { CHANGE_CLASSES, CHANGE_LABELS, CHANGE_LETTERS } from '../git/gitLabels'

const CONFLICT_LETTER = '!'
const CONFLICT_LABEL = 'En conflit'
const CONFLICT_CLASS = 'text-tily-error'
const FOLDER_LETTER = '•'
const FOLDER_LABEL = 'Contient des modifications'
const SEPARATOR = /[\\/]+/
const PATH_SEPARATOR = '\\'

export interface MarkView {
  letter: string
  label: string
  className: string
}

export const markKey = (path: string): string => path.replace(/[\\/]+$/, '').toLowerCase()

const precedence = (mark: GitPathMark): number => {
  if (mark.conflicted) {
    return 3
  }
  if (mark.kind === GitChangeKind.Untracked || mark.kind === GitChangeKind.Deleted) {
    return 2
  }
  return mark.kind === GitChangeKind.Added || mark.kind === GitChangeKind.Renamed || mark.kind === GitChangeKind.Copied ? 1 : 0
}

export const folderMarksOf = (root: string | null, marks: GitPathMark[]): Map<string, GitPathMark> => {
  const folders = new Map<string, GitPathMark>()
  if (!root) {
    return folders
  }
  const rootKey = markKey(root)
  for (const mark of marks) {
    const segments = markKey(mark.path).split(SEPARATOR)
    for (let length = segments.length - 1; length > 0; length--) {
      const folder = segments.slice(0, length).join(PATH_SEPARATOR)
      if (folder.length <= rootKey.length) {
        break
      }
      const existing = folders.get(folder)
      if (!existing || precedence(mark) > precedence(existing)) {
        folders.set(folder, mark)
      }
    }
  }
  return folders
}

export const fileMarkView = (mark: GitPathMark): MarkView =>
  mark.conflicted
    ? { letter: CONFLICT_LETTER, label: CONFLICT_LABEL, className: CONFLICT_CLASS }
    : { letter: CHANGE_LETTERS[mark.kind], label: CHANGE_LABELS[mark.kind], className: CHANGE_CLASSES[mark.kind] }

export const folderMarkView = (mark: GitPathMark): MarkView => ({ letter: FOLDER_LETTER, label: FOLDER_LABEL, className: fileMarkView(mark).className })
