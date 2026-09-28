import { useEffect, useRef } from 'react'

interface EmptyStateProps {
  canRestore: boolean
  onNewWorkspace: () => void
  onOpenProject: () => void
  onRestoreTab: () => void
}

const SECONDARY_BUTTON = 'cursor-pointer rounded border border-dock-line px-3 py-1.5 text-[13px] text-dock-ink hover:bg-dock-green-hover'

export function EmptyState({ canRestore, onNewWorkspace, onOpenProject, onRestoreTab }: EmptyStateProps) {
  const newWorkspaceRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (document.activeElement === document.body) {
      newWorkspaceRef.current?.focus()
    }
  }, [])

  return (
    <div className="flex h-full flex-col items-center justify-center gap-4 text-dock-muted">
      <div className="flex flex-col items-center gap-1">
        <p className="text-[15px]">Aucun workspace ouvert.</p>
        <p className="text-[12px]">Créez-en un pour lancer un terminal, ou ouvrez un dossier de projet.</p>
      </div>
      <div className="flex flex-wrap justify-center gap-3">
        <button ref={newWorkspaceRef} type="button" className="cursor-pointer rounded border border-dock-green px-3 py-1.5 text-[13px] text-dock-green-deep hover:bg-dock-green-soft" data-tip="Créer un workspace avec un terminal dans votre dossier utilisateur" onClick={onNewWorkspace}>
          Nouveau workspace
        </button>
        <button type="button" className={SECONDARY_BUTTON} data-tip="Ouvrir un workspace dans un dossier de projet" onClick={onOpenProject}>
          Ouvrir un projet…
        </button>
        {canRestore && (
          <button type="button" className={SECONDARY_BUTTON} data-tip="Rouvrir le dernier onglet fermé (Ctrl + Maj + Z)" onClick={onRestoreTab}>
            Rouvrir le dernier onglet fermé
          </button>
        )}
      </div>
    </div>
  )
}
