import { useEffect, useRef } from 'react'
import { Command } from '../features/keyboard/commands'
import { useLeaderText, useShortcutText, useShortcutTip } from '../features/keyboard/shortcutLabels'
import { BUTTON_PRIMARY, BUTTON_SECONDARY } from '../components/buttonStyles'

interface EmptyStateProps {
  canRestore: boolean
  onNewWorkspace: () => void
  onOpenProject: () => void
  onRestoreTab: () => void
}

export function EmptyState({ canRestore, onNewWorkspace, onOpenProject, onRestoreTab }: EmptyStateProps) {
  const newWorkspaceRef = useRef<HTMLButtonElement>(null)
  const newWorkspaceTip = useShortcutTip('Nouveau workspace', Command.NewWorkspace)
  const openProjectTip = useShortcutTip('Ouvrir un projet', Command.Projects)
  const restoreTip = useShortcutTip('Rouvrir le dernier onglet fermé', Command.RestoreTab)
  const paletteText = useShortcutText(Command.Palette)
  const leaderText = useLeaderText()

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
        <button ref={newWorkspaceRef} type="button" className={BUTTON_PRIMARY} data-tip={newWorkspaceTip} onClick={onNewWorkspace}>
          Nouveau workspace
        </button>
        <button type="button" className={BUTTON_SECONDARY} data-tip={openProjectTip} onClick={onOpenProject}>
          Ouvrir un projet…
        </button>
        {canRestore && (
          <button type="button" className={BUTTON_SECONDARY} data-tip={restoreTip} onClick={onRestoreTab}>
            Rouvrir le dernier onglet fermé
          </button>
        )}
      </div>
      <p className="text-[11px]">{paletteText ? `${paletteText} ouvre la palette · ` : ''}{leaderText} puis une lettre lance une commande au clavier</p>
    </div>
  )
}
