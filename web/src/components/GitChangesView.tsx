import { useMemo, useState, type KeyboardEvent } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { GitDiffSource, type GitState } from '../bridge/gitMessages'
import { focusGitRow } from '../git/gitFocus'
import { plural } from '../git/gitLabels'
import { discardChanges, openInEditor, resolveConflict, stageChanges, unstageChanges } from '../git/gitRequests'
import { changeRows, drawerShowsWorkingFile, GitRowGroup, openChangeRow, rowKey, type GitChangeRow, type GitRowHandlers } from '../git/gitRows'
import { useGitStore } from '../store/gitStore'
import { GitCommitBox } from './GitCommitBox'
import { GitFileRow } from './GitFileRow'
import { GitGroupHeader } from './GitGroupHeader'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { ROW_ACTION } from './rightPanelStyles'

interface GitChangesViewProps {
  state: GitState
  busy: string | null
}

const KEYBOARD_TIP = 'Entrée : diff · Espace : stage ou unstage · Suppr : abandonner'

const hiddenNote = (shown: number, total: number) =>
  total > shown ? <p className="py-[2px] pl-[30px] text-[11px] text-dock-muted italic">{`… et ${plural(total - shown, 'autre fichier non affiché', 'autres fichiers non affichés')}`}</p> : null

const toggleRow = (row: GitChangeRow): void => {
  if (row.conflict) {
    resolveConflict(row.conflict)
  } else if (row.change) {
    if (row.group === GitRowGroup.Staged) {
      unstageChanges([row.change])
    } else {
      stageChanges([row.change])
    }
  }
}

export function GitChangesView({ state, busy }: GitChangesViewProps) {
  const { file, commit } = useGitStore(useShallow((store) => ({ file: store.file, commit: store.commit })))
  const [focusKey, setFocusKey] = useState<string | null>(null)
  const rows = useMemo(() => changeRows(state.conflicts, state.staged, state.unstaged), [state.conflicts, state.staged, state.unstaged])
  const selectedKey = file && !commit ? rowKey(file.source === GitDiffSource.Staged ? GitRowGroup.Staged : GitRowGroup.Unstaged, file.path) : null
  const focusableKey = rows.find((row) => row.key === focusKey)?.key ?? rows.find((row) => row.key === selectedKey)?.key ?? rows[0]?.key
  const handlers: GitRowHandlers = useMemo(
    () => ({
      open: openChangeRow,
      stage: (change) => stageChanges([change]),
      unstage: (change) => unstageChanges([change]),
      discard: (change) => discardChanges([change], 1),
      edit: openInEditor,
      resolve: resolveConflict,
      focus: setFocusKey,
    }),
    [],
  )
  const empty = rows.length === 0

  const moveTo = (row: GitChangeRow | undefined) => {
    if (!row) {
      return
    }
    setFocusKey(row.key)
    focusGitRow(row.key)
    if (drawerShowsWorkingFile() && !row.conflict) {
      openChangeRow(row)
    }
  }
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = rows.findIndex((row) => row.key === (event.target as HTMLElement).dataset.gitRow)
    const current = rows[index]
    if (event.key === 'ArrowDown') {
      moveTo(rows[index < 0 ? 0 : Math.min(index + 1, rows.length - 1)])
    } else if (event.key === 'ArrowUp') {
      moveTo(rows[Math.max(index - 1, 0)])
    } else if (event.key === 'Home') {
      moveTo(rows[0])
    } else if (event.key === 'End') {
      moveTo(rows.at(-1))
    } else if (!current) {
      return
    } else if (event.key === 'Enter') {
      openChangeRow(current)
    } else if (event.key === ' ') {
      toggleRow(current)
    } else if (event.key === 'Delete' && current.group === GitRowGroup.Unstaged && current.change) {
      discardChanges([current.change], 1)
    } else {
      return
    }
    event.preventDefault()
    event.stopPropagation()
  }
  const handleStageAll = () => stageChanges([])
  const handleUnstageAll = () => unstageChanges([])
  const handleDiscardAll = () => discardChanges([], state.unstagedTotal)
  const renderRows = (group: GitRowGroup) =>
    rows
      .filter((row) => row.group === group)
      .map((row) => <GitFileRow key={row.key} row={row} selected={row.key === selectedKey} focusable={row.key === focusableKey} handlers={handlers} />)

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div role="listbox" aria-label="Modifications du dépôt" className="min-h-0 flex-1 overflow-auto py-[4px]" onKeyDown={handleKeyDown}>
        {empty && <p className="px-[12px] py-[6px] text-[12px] text-dock-muted italic">Aucune modification : l’arbre de travail est propre.</p>}
        {state.conflicts.length > 0 && (
          <>
            <GitGroupHeader title="Conflits" count={state.conflicts.length} tip="Ouvrez chaque fichier dans l’éditeur, résolvez-le puis marquez-le résolu" />
            {renderRows(GitRowGroup.Conflict)}
          </>
        )}
        {state.stagedTotal > 0 && (
          <>
            <GitGroupHeader title="Staged" count={state.stagedTotal} tip={KEYBOARD_TIP}>
              <button type="button" className={ROW_ACTION} aria-label="Unstage de tout" data-tip="Unstage de tout" onClick={handleUnstageAll}>
                <Icon name={IconName.Minus} />
              </button>
            </GitGroupHeader>
            {renderRows(GitRowGroup.Staged)}
            {hiddenNote(state.staged.length, state.stagedTotal)}
          </>
        )}
        {state.unstagedTotal > 0 && (
          <>
            <GitGroupHeader title="Unstaged" count={state.unstagedTotal} tip={KEYBOARD_TIP}>
              <button type="button" className={`${ROW_ACTION} hover:text-dock-error`} aria-label="Tout abandonner" data-tip="Abandonner toutes les modifications unstaged" onClick={handleDiscardAll}>
                <Icon name={IconName.Discard} />
              </button>
              <button type="button" className={ROW_ACTION} aria-label="Stage de tout" data-tip="Stage de tout" onClick={handleStageAll}>
                <Icon name={IconName.Plus} />
              </button>
            </GitGroupHeader>
            {renderRows(GitRowGroup.Unstaged)}
            {hiddenNote(state.unstaged.length, state.unstagedTotal)}
          </>
        )}
      </div>
      <GitCommitBox state={state} busy={busy} />
    </div>
  )
}
