import { GitDiffLineKind, type GitDiff } from '../bridge/gitMessages'
import { fileName, plural } from './gitLabels'
import { copyToClipboard } from './gitRequests'

type CopyableDiff = GitDiff & { patch: string }

export const canCopyDiff = (diff: GitDiff | null): diff is CopyableDiff => Boolean(diff?.patch)

export const copyDiffTip = (diff: GitDiff | null): string => {
  if (canCopyDiff(diff)) {
    return 'Copier le diff (format unifié, applicable par git apply)'
  }
  return diff?.truncated ? 'Diff tronqué à l’affichage : copie impossible' : 'Aucun diff texte à copier'
}

export const copyDiff = (diff: GitDiff | null): void => {
  if (!canCopyDiff(diff)) {
    return
  }
  const changed = diff.hunks.flatMap((hunk) => hunk.lines).filter((line) => line.kind === GitDiffLineKind.Added || line.kind === GitDiffLineKind.Removed).length
  copyToClipboard(diff.patch, `Diff de ${fileName(diff.path)} copié (${plural(changed, 'ligne modifiée', 'lignes modifiées')}).`)
}
