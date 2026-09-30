import { GitDiffLineKind, type GitDiff, type GitDiffLine } from '../bridge/gitMessages'
import { fileName, plural } from './gitLabels'
import { copyToClipboard } from './gitRequests'

const NO_NEWLINE = '\\ No newline at end of file'
const LINE_BREAK = '\n'

const PREFIXES: Record<GitDiffLineKind, string> = {
  [GitDiffLineKind.Context]: ' ',
  [GitDiffLineKind.Added]: '+',
  [GitDiffLineKind.Removed]: '-',
  [GitDiffLineKind.Note]: '',
}

const lineText = (line: GitDiffLine): string => (line.kind === GitDiffLineKind.Note ? NO_NEWLINE : `${PREFIXES[line.kind]}${line.text}`)

export const canCopyDiff = (diff: GitDiff | null): diff is GitDiff => diff !== null && !diff.binary && diff.hunks.length > 0

export const unifiedDiffText = (diff: GitDiff): string =>
  [`--- a/${diff.oldPath ?? diff.path}`, `+++ b/${diff.path}`, ...diff.hunks.flatMap((hunk) => [hunk.header, ...hunk.lines.map(lineText)])].join(LINE_BREAK) + LINE_BREAK

export const copyDiff = (diff: GitDiff | null): void => {
  if (!canCopyDiff(diff)) {
    return
  }
  const text = unifiedDiffText(diff)
  const changed = diff.hunks.flatMap((hunk) => hunk.lines).filter((line) => line.kind === GitDiffLineKind.Added || line.kind === GitDiffLineKind.Removed).length
  copyToClipboard(text, `Diff de ${fileName(diff.path)} copié (${plural(changed, 'ligne modifiée', 'lignes modifiées')}).`)
}
