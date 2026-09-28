import { GitChangeKind, type GitState } from '../bridge/gitMessages'
import type { ActionMenuItem } from '../components/ActionMenu'
import { absolutePath, fileName } from './gitLabels'
import { promptStashFiles } from './gitRefActions'
import { copyToClipboard, discardChanges, ignoreFiles, openInEditor, resolveConflicts, stageChanges, unstageChanges, withOldPaths } from './gitRequests'
import { GitRowGroup, rowPath, type GitChangeRow } from './gitRows'

const byCount = (count: number, one: string, several: string): string => (count > 1 ? several : one)

const unique = (paths: string[]): string[] => [...new Set(paths)]

export const selectedChanges = (rows: GitChangeRow[]) => ({
  conflicts: rows.flatMap((row) => (row.conflict ? [row.conflict] : [])),
  staged: rows.flatMap((row) => (row.group === GitRowGroup.Staged && row.change ? [row.change] : [])),
  unstaged: rows.flatMap((row) => (row.group === GitRowGroup.Unstaged && row.change ? [row.change] : [])),
})

export const toggleSelection = (rows: GitChangeRow[]): void => {
  const { conflicts, staged, unstaged } = selectedChanges(rows)
  if (unstaged.length > 0) {
    stageChanges(unstaged)
  } else if (staged.length > 0) {
    unstageChanges(staged)
  } else if (conflicts.length > 0) {
    resolveConflicts(conflicts)
  }
}

export const discardSelection = (rows: GitChangeRow[]): void => {
  const { unstaged } = selectedChanges(rows)
  if (unstaged.length > 0) {
    discardChanges(unstaged, unstaged.length)
  }
}

export const changeMenuLabel = (rows: GitChangeRow[]): string => (rows.length === 1 ? `Actions de ${fileName(rowPath(rows[0]))}` : `Actions de ${rows.length} fichiers`)

export const changeMenu = (rows: GitChangeRow[], state: GitState): ActionMenuItem[] => {
  const { conflicts, staged, unstaged } = selectedChanges(rows)
  const editable = unique(rows.filter((row) => row.change?.kind !== GitChangeKind.Deleted).map(rowPath))
  const stashable = unique(withOldPaths([...staged, ...unstaged]))
  const untracked = unstaged.filter((change) => change.kind === GitChangeKind.Untracked).map((change) => change.path)
  const paths = unique(rows.map(rowPath))
  const copied = paths.map((path) => absolutePath(state.root, path)).join('\n')
  const items: (ActionMenuItem | false)[] = [
    editable.length > 0 && { id: 'edit', label: byCount(editable.length, 'Ouvrir dans l’éditeur', `Ouvrir ${editable.length} fichiers dans l’éditeur`), run: () => editable.forEach(openInEditor) },
    unstaged.length > 0 && { id: 'stage', label: byCount(unstaged.length, 'Stage', `Stage de ${unstaged.length} fichiers`), run: () => stageChanges(unstaged) },
    staged.length > 0 && { id: 'unstage', label: byCount(staged.length, 'Unstage', `Unstage de ${staged.length} fichiers`), run: () => unstageChanges(staged) },
    conflicts.length > 0 && { id: 'resolve', label: byCount(conflicts.length, 'Marquer résolu', `Marquer ${conflicts.length} fichiers résolus`), run: () => resolveConflicts(conflicts) },
    stashable.length > 0 && {
      id: 'stash',
      label: byCount(stashable.length, 'Stash du fichier…', `Stash de ${stashable.length} fichiers…`),
      disabled: Boolean(state.operation),
      run: () => promptStashFiles(stashable),
    },
    unstaged.length > 0 && {
      id: 'discard',
      label: byCount(unstaged.length, 'Abandonner les modifications', `Abandonner les modifications de ${unstaged.length} fichiers`),
      run: () => discardChanges(unstaged, unstaged.length),
    },
    untracked.length > 0 && { id: 'ignore', label: byCount(untracked.length, 'Ajouter au .gitignore', `Ajouter ${untracked.length} fichiers au .gitignore`), run: () => ignoreFiles(untracked) },
    { id: 'copy', label: byCount(paths.length, 'Copier le chemin', `Copier les ${paths.length} chemins`), run: () => copyToClipboard(copied, byCount(paths.length, 'Chemin copié.', `${paths.length} chemins copiés.`)) },
  ]
  return items.filter((item): item is ActionMenuItem => item !== false)
}
