import { useEffect, useRef, type ChangeEvent, type PointerEvent } from 'react'
import type { WorktreeRemoval } from '../store/worktreeStore'
import { cancelWorktreeRemoval, changeWorktreeRemoval, confirmWorktreeRemoval } from '../worktree/worktreeActions'
import { keepTabInside } from './focusTrap'
import { SETTINGS_BUTTON, SETTINGS_HINT, SETTINGS_SECONDARY } from './settingsStyles'
import { WorktreeFailureDetails } from './WorktreeFailureDetails'

interface WorktreeRemoveDialogProps {
  removal: WorktreeRemoval
}

const DANGER = `${SETTINGS_BUTTON} border-dock-error text-dock-error hover:bg-dock-green-hover`
const CHECK_LABEL = 'flex items-center gap-2 text-[12px] text-dock-ink'

const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
  if (event.target === event.currentTarget) {
    cancelWorktreeRemoval()
  }
}

const handleClosePanesChange = (event: ChangeEvent<HTMLInputElement>) => changeWorktreeRemoval({ closePanes: event.target.checked })
const handleKeepBranchChange = (event: ChangeEvent<HTMLInputElement>) => changeWorktreeRemoval({ keepBranch: event.target.checked })
const handleDropDatabaseChange = (event: ChangeEvent<HTMLInputElement>) => changeWorktreeRemoval({ dropDatabase: event.target.checked })

export function WorktreeRemoveDialog({ removal }: WorktreeRemoveDialogProps) {
  const { name, path, branch, panes, closePanes, keepBranch, dropDatabase, failure } = removal
  const confirmRef = useRef<HTMLButtonElement>(null)
  const title = `Supprimer le worktree « ${name} » ?`

  useEffect(() => {
    confirmRef.current?.focus()
    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        cancelWorktreeRemoval()
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => document.removeEventListener('keydown', handleDocumentKeyDown)
  }, [])

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-dock-paper/60 pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="alertdialog" aria-label={title} className="flex max-h-[76vh] w-[540px] max-w-[94vw] flex-col rounded-lg border border-dock-line bg-dock-panel shadow-xl" onKeyDown={keepTabInside}>
        <div className="flex items-center justify-between border-b border-dock-line px-4 py-3">
          <h2 className="truncate text-[15px] font-semibold text-dock-ink">{title}</h2>
          <span className="shrink-0 pl-3 text-[11px] text-dock-muted">Entrée supprime · Échap annule</span>
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4 py-4">
          <p className={`${SETTINGS_HINT} font-mono`}>{path}</p>
          <p className="text-[12px] text-dock-warning">Le dossier est supprimé avec ses modifications non commitées.</p>
          {panes.length > 0 && (
            <>
              <ul className="flex flex-col gap-1">
                {panes.map((pane) => (
                  <li key={pane.paneId} className="flex items-center gap-2 rounded border border-dock-line bg-dock-paper px-3 py-1.5 text-[12px] text-dock-ink">
                    <span className="min-w-0 flex-1 truncate">{pane.label}</span>
                    {pane.agent && <span className="shrink-0 text-[11px] text-dock-warning">agent actif</span>}
                  </li>
                ))}
              </ul>
              <label className={CHECK_LABEL}>
                <input type="checkbox" checked={closePanes} onChange={handleClosePanesChange} />
                Fermer ces onglets et panes (leurs programmes sont arrêtés)
              </label>
              {!closePanes && <p className="text-[11px] text-dock-warning">Un shell ouvert dans le dossier le verrouille : la suppression échouera probablement.</p>}
            </>
          )}
          {branch && (
            <label className={CHECK_LABEL}>
              <input type="checkbox" checked={keepBranch} onChange={handleKeepBranchChange} />
              {`Garder la branche « ${branch} »`}
            </label>
          )}
          <label className={CHECK_LABEL}>
            <input type="checkbox" checked={dropDatabase} onChange={handleDropDatabaseChange} />
            Supprimer la base répliquée (jamais celle du dépôt principal)
          </label>
          {failure && <WorktreeFailureDetails failure={failure} />}
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-dock-line px-4 py-3">
          <button type="button" className={SETTINGS_SECONDARY} onClick={cancelWorktreeRemoval}>
            Annuler
          </button>
          <button ref={confirmRef} type="button" className={DANGER} onClick={confirmWorktreeRemoval}>
            {failure ? 'Réessayer' : 'Supprimer'}
          </button>
        </div>
      </div>
    </div>
  )
}
