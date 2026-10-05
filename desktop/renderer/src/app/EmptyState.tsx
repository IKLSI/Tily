import { useEffect, useRef } from 'react'

interface EmptyStateProps {
  canRestore: boolean
  onNewWorkspace: () => void
  onOpenProject: () => void
  onRestoreTab: () => void
}

const SECONDARY_BUTTON = 'cursor-pointer rounded border border-tily-line px-3 py-1.5 text-[13px] text-tily-ink hover:bg-tily-green-hover'

export function EmptyState({ canRestore, onNewWorkspace, onOpenProject, onRestoreTab }: EmptyStateProps) {
  const newWorkspaceRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (document.activeElement === document.body) {
      newWorkspaceRef.current?.focus()
    }
  }, [])

  return (
    <div className="flex h-full flex-col items-center justify-center gap-4 text-tily-muted">
      <div className="flex flex-col items-center gap-1">
        <p className="text-[15px]">Aucun workspace ouvert.</p>
        <p className="text-[12px]">Créez-en un pour lancer un terminal, ou ouvrez un dossier de projet.</p>
      </div>
      <div className="flex flex-wrap justify-center gap-3">
        <button ref={newWorkspaceRef} type="button" className="cursor-pointer rounded border border-tily-green px-3 py-1.5 text-[13px] text-tily-green-deep hover:bg-tily-green-soft" data-tip="Créer un workspace avec un terminal dans votre dossier utilisateur (Ctrl + Maj + W)" onClick={onNewWorkspace}>
          Nouveau workspace
        </button>
        <button type="button" className={SECONDARY_BUTTON} data-tip="Ouvrir un workspace dans un dossier de projet (Leader puis F)" onClick={onOpenProject}>
          Ouvrir un projet…
        </button>
        {canRestore && (
          <button type="button" className={SECONDARY_BUTTON} data-tip="Rouvrir le dernier onglet fermé (Ctrl + Maj + Z)" onClick={onRestoreTab}>
            Rouvrir le dernier onglet fermé
          </button>
        )}
      </div>
      <p className="text-[11px]">Ctrl + P ouvre la palette · Cmd + K puis une lettre lance une commande au clavier</p>
    </div>
  )
}
