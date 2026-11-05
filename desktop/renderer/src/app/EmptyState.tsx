import { useEffect, useRef } from 'react'
import { Command } from '../features/keyboard/commands'
import { useLeaderText, useShortcutText, useShortcutTip } from '../features/keyboard/shortcutLabels'
import { BUTTON_PRIMARY, BUTTON_SECONDARY } from '../components/buttonStyles'

const KBD = 'rounded-tily-sm border border-tily-line bg-tily-raised px-[6px] py-[2px] font-sans text-tily-xs text-tily-ink-soft'

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
    <div className="flex h-full flex-col items-center justify-center gap-[24px] text-tily-muted">
      <svg width="44" height="44" viewBox="0 0 44 44" fill="none" aria-hidden="true" className="text-tily-green">
        <rect x="1" y="1" width="42" height="42" rx="12" className="fill-tily-raised stroke-tily-line" strokeWidth="1" />
        <path d="M13 15l8 7-8 7" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
        <path d="M24 29h8" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" />
      </svg>
      <div className="flex flex-col items-center gap-[6px]">
        <p className="text-tily-lg font-semibold text-tily-ink">Aucun workspace ouvert</p>
        <p className="text-tily-sm">Créez-en un pour lancer un terminal, ou ouvrez un dossier de projet.</p>
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
      <p className="flex flex-wrap items-center justify-center gap-[6px] text-tily-xs">
        {paletteText && (
          <>
            <kbd className={KBD}>{paletteText}</kbd>
            <span>palette</span>
            <span aria-hidden="true">·</span>
          </>
        )}
        <kbd className={KBD}>{leaderText}</kbd>
        <span>puis une lettre lance une commande au clavier</span>
      </p>
    </div>
  )
}
