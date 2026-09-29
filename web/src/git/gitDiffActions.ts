import { GitDiffSource, type GitDiff } from '../bridge/gitMessages'
import { useGitStore, type GitDiffSelection, type GitFileTarget } from '../store/gitStore'
import { changeRowsBetween, diffRowsOf, DiffRowKind, hunkChangeRows, hunkHeaderRow, isChangeRow, nextChangeRow, toHunkSelection, type DiffRow } from './gitDiffRows'
import { focusGitDiff } from './gitFocus'
import { fileName, plural } from './gitLabels'
import { askConfirmation, withRoot } from './gitRequests'

export enum DiffPick {
  Replace = 'replace',
  Toggle = 'toggle',
  Extend = 'extend',
}

export enum DiffLineAction {
  Stage = 'stage',
  Unstage = 'unstage',
  Discard = 'discard',
}

const MESSAGE_TYPES = {
  [DiffLineAction.Stage]: 'git.stageLines',
  [DiffLineAction.Unstage]: 'git.unstageLines',
  [DiffLineAction.Discard]: 'git.discardLines',
} as const

interface DiffContext {
  diff: GitDiff
  fingerprint: string
  file: GitFileTarget
  rows: DiffRow[]
  selection: GitDiffSelection
}

export const isDiffSelectable = (diff: GitDiff | null, file: GitFileTarget | null): boolean => Boolean(diff?.fingerprint && file && !file.commit)

export const diffActionsFor = (source: GitDiffSource): DiffLineAction[] => (source === GitDiffSource.Staged ? [DiffLineAction.Unstage] : [DiffLineAction.Discard, DiffLineAction.Stage])

const currentContext = (): DiffContext | null => {
  const { diff, file, diffSelection } = useGitStore.getState()
  return diff?.fingerprint && file && !file.commit ? { diff, fingerprint: diff.fingerprint, file, rows: diffRowsOf(diff), selection: diffSelection } : null
}

const select = (rows: Iterable<number>, anchor: number | null, cursor: number | null): void => useGitStore.getState().setDiffSelection({ rows: new Set(rows), anchor, cursor })

export const selectDiffHunk = (hunk: number): void => {
  const context = currentContext()
  const rows = context ? hunkChangeRows(context.rows, hunk) : []
  if (rows.length > 0) {
    select(rows, rows[0], rows[0])
  }
}

export const pickDiffRow = (row: number, pick: DiffPick): void => {
  const context = currentContext()
  const target = context?.rows[row]
  if (!context || !target) {
    return
  }
  if (target.kind === DiffRowKind.Hunk && target.hunk !== undefined) {
    selectDiffHunk(target.hunk)
    return
  }
  const { selection } = context
  if (pick === DiffPick.Extend && selection.anchor !== null) {
    select(changeRowsBetween(context.rows, selection.anchor, row), selection.anchor, isChangeRow(target) ? row : selection.cursor)
    return
  }
  if (!isChangeRow(target)) {
    return
  }
  if (pick === DiffPick.Toggle) {
    const rows = new Set(selection.rows)
    if (rows.has(row)) {
      rows.delete(row)
    } else {
      rows.add(row)
    }
    select(rows, row, row)
    return
  }
  select([row], row, row)
}

export const moveDiffCursor = (step: 1 | -1, extend: boolean, topRow: number): void => {
  const context = currentContext()
  if (!context) {
    return
  }
  const { selection, rows } = context
  const next = nextChangeRow(rows, selection.cursor ?? topRow - step, step)
  if (next === null) {
    return
  }
  const anchor = extend ? (selection.anchor ?? next) : next
  select(extend ? changeRowsBetween(rows, anchor, next) : [next], anchor, next)
}

export const moveDiffHunk = (step: 1 | -1, topRow: number): void => {
  const context = currentContext()
  if (!context) {
    return
  }
  const { selection, rows, diff } = context
  const cursorHunk = selection.cursor === null ? undefined : rows[selection.cursor]?.hunk
  const hunks = diff.hunks.map((_, index) => index)
  const target = cursorHunk === undefined ? (step > 0 ? hunks.find((hunk) => hunkHeaderRow(rows, hunk) >= topRow) : hunks.findLast((hunk) => hunkHeaderRow(rows, hunk) < topRow)) : cursorHunk + step
  if (target !== undefined && target >= 0 && target < hunks.length) {
    selectDiffHunk(target)
  }
}

export const selectAllDiffLines = (): void => {
  const context = currentContext()
  const rows = context ? changeRowsBetween(context.rows, 0, context.rows.length - 1) : []
  if (context && rows.length > 0) {
    select(rows, rows[0], context.selection.cursor ?? rows[0])
  }
}

export const clearDiffSelection = (): boolean => {
  const context = currentContext()
  if (!context || context.selection.rows.size === 0) {
    return false
  }
  select([], null, context.selection.cursor)
  return true
}

const applyRows = (action: DiffLineAction, rows: number[]): void => {
  const context = currentContext()
  if (!context || !diffActionsFor(context.file.source).includes(action)) {
    return
  }
  const { file, fingerprint } = context
  const selection = toHunkSelection(context.rows, rows)
  if (selection.length === 0) {
    return
  }
  const run = (confirmed: boolean) => withRoot((path) => ({ type: MESSAGE_TYPES[action], path, file: file.path, untracked: file.untracked, fingerprint, selection, confirmed }), false)
  if (action !== DiffLineAction.Discard) {
    run(false)
    return
  }
  const count = selection.reduce((total, hunk) => total + hunk.lines.length, 0)
  askConfirmation({
    title: `Abandonner ${plural(count, 'ligne modifiée', 'lignes modifiées')} de « ${fileName(file.path)} » ?`,
    body: file.untracked ? 'Les lignes choisies seront retirées de ce fichier non suivi.' : 'Les lignes choisies reviennent à leur version staged, sinon à celle du dernier commit.',
    detail: `${file.path}\n« Annuler » dans la vue Git peut encore les restaurer tant qu’aucune autre opération n’est faite.`,
    confirmLabel: 'Abandonner',
    run: () => run(true),
    restoreFocus: focusGitDiff,
  })
}

export const applyDiffSelection = (action: DiffLineAction): void => applyRows(action, [...useGitStore.getState().diffSelection.rows])

export const applyDiffHunk = (hunk: number, action: DiffLineAction): void => {
  const context = currentContext()
  if (context) {
    applyRows(action, hunkChangeRows(context.rows, hunk))
  }
}

export const applyDiffLine = (row: number, action: DiffLineAction): void => {
  if (useGitStore.getState().diffSelection.rows.has(row)) {
    applyDiffSelection(action)
  } else {
    applyRows(action, [row])
  }
}

export const toggleStageDiffSelection = (): void => {
  const file = useGitStore.getState().file
  if (file) {
    applyDiffSelection(file.source === GitDiffSource.Staged ? DiffLineAction.Unstage : DiffLineAction.Stage)
  }
}
