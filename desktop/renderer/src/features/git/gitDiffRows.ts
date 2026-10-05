import { GitDiffLineKind, type GitDiff, type GitDiffLine, type GitHunkSelection } from '../../bridge/gitMessages'

export enum DiffRowKind {
  Note = 'note',
  Hunk = 'hunk',
  Line = 'line',
}

export interface DiffRow {
  kind: DiffRowKind
  text: string
  line?: GitDiffLine
  hunk?: number
  index?: number
}

const TRUNCATED_NOTE = 'Diff tronqué : ouvrez le fichier dans l’éditeur pour le voir en entier.'

const rowsCache = new WeakMap<GitDiff, DiffRow[]>()

const flatten = (diff: GitDiff): DiffRow[] => {
  const rows: DiffRow[] = diff.notes.map((note) => ({ kind: DiffRowKind.Note, text: note }))
  diff.hunks.forEach((hunk, hunkIndex) => {
    rows.push({ kind: DiffRowKind.Hunk, text: hunk.header, hunk: hunkIndex })
    hunk.lines.forEach((line, index) => rows.push({ kind: DiffRowKind.Line, text: line.text, line, hunk: hunkIndex, index }))
  })
  if (!diff.binary && diff.hunks.length === 0 && diff.notes.length === 0) {
    rows.push({ kind: DiffRowKind.Note, text: 'Aucune différence de contenu.' })
  }
  if (diff.truncated) {
    rows.push({ kind: DiffRowKind.Note, text: TRUNCATED_NOTE })
  }
  return rows
}

export const diffRowsOf = (diff: GitDiff): DiffRow[] => {
  const cached = rowsCache.get(diff)
  if (cached) {
    return cached
  }
  const rows = flatten(diff)
  rowsCache.set(diff, rows)
  return rows
}

export const isChangeRow = (row: DiffRow | undefined): boolean => row?.line?.kind === GitDiffLineKind.Added || row?.line?.kind === GitDiffLineKind.Removed

export const changeRowsBetween = (rows: DiffRow[], from: number, to: number): number[] => {
  const result: number[] = []
  for (let row = Math.max(0, Math.min(from, to)); row <= Math.min(rows.length - 1, Math.max(from, to)); row++) {
    if (isChangeRow(rows[row])) {
      result.push(row)
    }
  }
  return result
}

export const hunkChangeRows = (rows: DiffRow[], hunk: number): number[] => rows.flatMap((row, index) => (row.hunk === hunk && isChangeRow(row) ? [index] : []))

export const hunkHeaderRow = (rows: DiffRow[], hunk: number): number => rows.findIndex((row) => row.kind === DiffRowKind.Hunk && row.hunk === hunk)

export const nextChangeRow = (rows: DiffRow[], from: number, step: 1 | -1): number | null => {
  for (let row = from + step; row >= 0 && row < rows.length; row += step) {
    if (isChangeRow(rows[row])) {
      return row
    }
  }
  return null
}

export const toHunkSelection = (rows: DiffRow[], selected: Iterable<number>): GitHunkSelection[] => {
  const byHunk = new Map<number, number[]>()
  for (const index of [...selected].sort((a, b) => a - b)) {
    const row = rows[index]
    if (isChangeRow(row) && row.hunk !== undefined && row.index !== undefined) {
      byHunk.set(row.hunk, [...(byHunk.get(row.hunk) ?? []), row.index])
    }
  }
  return [...byHunk].map(([hunk, lines]) => ({ hunk, lines }))
}
