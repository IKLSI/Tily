import { useMemo, useRef } from 'react'
import { GitDiffLineKind, type GitDiff, type GitDiffLine } from '../bridge/gitMessages'
import { useVirtualRows } from './useVirtualRows'

interface GitDiffViewProps {
  diff: GitDiff | null
  error: string | null
  placeholder: string
}

enum DiffRowKind {
  Note = 'note',
  Hunk = 'hunk',
  Line = 'line',
}

interface DiffRow {
  kind: DiffRowKind
  text: string
  line?: GitDiffLine
}

const LINE_HEIGHT = 18
const TRUNCATED_NOTE = 'Diff tronqué : ouvrez le fichier dans l’éditeur pour le voir en entier.'

const LINE_CLASSES: Record<GitDiffLineKind, string> = {
  [GitDiffLineKind.Context]: 'text-dock-terminal-ink',
  [GitDiffLineKind.Added]: 'bg-dock-diff-added text-dock-diff-added-ink',
  [GitDiffLineKind.Removed]: 'bg-dock-diff-removed text-dock-diff-removed-ink',
  [GitDiffLineKind.Note]: 'text-dock-muted italic',
}

const LINE_SIGNS: Record<GitDiffLineKind, string> = {
  [GitDiffLineKind.Context]: ' ',
  [GitDiffLineKind.Added]: '+',
  [GitDiffLineKind.Removed]: '−',
  [GitDiffLineKind.Note]: ' ',
}

const flatten = (diff: GitDiff): DiffRow[] => {
  const rows: DiffRow[] = diff.notes.map((note) => ({ kind: DiffRowKind.Note, text: note }))
  for (const hunk of diff.hunks) {
    rows.push({ kind: DiffRowKind.Hunk, text: hunk.header })
    for (const line of hunk.lines) {
      rows.push({ kind: DiffRowKind.Line, text: line.text, line })
    }
  }
  if (!diff.binary && diff.hunks.length === 0 && diff.notes.length === 0) {
    rows.push({ kind: DiffRowKind.Note, text: 'Aucune différence de contenu.' })
  }
  if (diff.truncated) {
    rows.push({ kind: DiffRowKind.Note, text: TRUNCATED_NOTE })
  }
  return rows
}

const renderRow = (row: DiffRow, index: number) => {
  const style = { top: index * LINE_HEIGHT, height: LINE_HEIGHT }
  if (row.kind === DiffRowKind.Hunk) {
    return (
      <div key={index} className="absolute left-0 w-max min-w-full bg-dock-panel px-[10px] text-dock-lane-1" style={style}>
        {row.text}
      </div>
    )
  }
  if (!row.line) {
    return (
      <div key={index} className="absolute left-0 w-max min-w-full px-[10px] font-sans text-dock-muted italic" style={style}>
        {row.text}
      </div>
    )
  }
  const { kind, old, new: next } = row.line
  return (
    <div key={index} className={`absolute left-0 flex w-max min-w-full ${LINE_CLASSES[kind]}`} style={style}>
      <span className="w-[46px] shrink-0 pr-[8px] text-right text-dock-muted select-none">{old ?? ''}</span>
      <span className="w-[46px] shrink-0 pr-[8px] text-right text-dock-muted select-none">{next ?? ''}</span>
      <span className="w-[16px] shrink-0 select-none">{LINE_SIGNS[kind]}</span>
      <span className="pr-[16px] whitespace-pre">{row.text}</span>
    </div>
  )
}

export function GitDiffView({ diff, error, placeholder }: GitDiffViewProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const rows = useMemo(() => (diff ? flatten(diff) : []), [diff])
  const { range, handleScroll } = useVirtualRows(containerRef, rows.length, LINE_HEIGHT)

  if (error || !diff) {
    return <p className={`px-[12px] py-[10px] text-[12px] ${error ? 'text-dock-error' : 'text-dock-muted italic'}`}>{error ?? placeholder}</p>
  }

  return (
    <div ref={containerRef} tabIndex={0} aria-label={`Diff de ${diff.path}`} className="relative min-h-0 flex-1 overflow-auto bg-dock-terminal font-mono text-[12px] leading-[18px] [tab-size:4]" onScroll={handleScroll}>
      <div className="relative" style={{ height: rows.length * LINE_HEIGHT }}>
        {rows.slice(range.start, range.end).map((row, offset) => renderRow(row, range.start + offset))}
      </div>
    </div>
  )
}
