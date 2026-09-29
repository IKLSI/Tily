import { useCallback, useEffect, useRef, useState, type FocusEvent } from 'react'
import type { ShellProfile } from '../bridge/messages'
import { PaneStateKind, type PaneState } from '../store/paneStore'
import { ShellMenu } from './ShellMenu'

interface PaneOverlayProps {
  state: PaneState
  shells: ShellProfile[]
  onRestart: () => void
  onRestartIn: (path: string) => void
  onChangeShell: (shellId: string) => void
  onDismiss: () => void
  onClose: () => void
}

const TITLES: Record<PaneStateKind, string> = {
  [PaneStateKind.Failed]: 'Le shell n’a pas pu démarrer.',
  [PaneStateKind.Exited]: 'Le shell est terminé.',
  [PaneStateKind.PathMissing]: 'Le dossier de ce pane n’existe plus.',
}

const PANE_SELECTOR = '[data-pane-id]'

const BUTTON = 'cursor-pointer rounded border px-3 py-1.5 text-[12px]'
const PRIMARY = `${BUTTON} border-dock-green text-dock-green-deep hover:bg-dock-green-soft`
const SECONDARY = `${BUTTON} border-dock-line text-dock-ink hover:bg-dock-green-hover`
const DANGER = `${BUTTON} border-dock-line text-dock-muted hover:text-dock-error`

export function PaneOverlay({ state, shells, onRestart, onRestartIn, onChangeShell, onDismiss, onClose }: PaneOverlayProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const overlayRef = useRef<HTMLDivElement>(null)
  const defaultRef = useRef<HTMLButtonElement>(null)
  const shellButtonRef = useRef<HTMLButtonElement>(null)
  const pathMissing = state.kind === PaneStateKind.PathMissing

  useEffect(() => {
    const focused = document.activeElement
    const pane = overlayRef.current?.closest(PANE_SELECTOR)
    if (focused === document.body || (focused && pane?.contains(focused))) {
      defaultRef.current?.focus()
    }
  }, [pathMissing])

  const handleOpenMenu = () => setMenuOpen(true)
  const handleCloseMenu = useCallback(() => {
    setMenuOpen(false)
    shellButtonRef.current?.focus()
  }, [])
  const handleSelectShell = (shellId: string) => {
    setMenuOpen(false)
    onChangeShell(shellId)
  }
  const handleRestartInFallback = () => {
    if (state.fallback) {
      onRestartIn(state.fallback)
    }
  }
  const handleOverlayFocus = (event: FocusEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget && !menuOpen) {
      defaultRef.current?.focus()
    }
  }
  return (
    <div ref={overlayRef} role="alert" tabIndex={-1} className="absolute inset-0 z-10 flex flex-col items-center justify-center gap-3 bg-dock-terminal/90 p-4 text-center" onFocus={handleOverlayFocus}>
      <p className="text-[13px] font-semibold text-dock-ink">{TITLES[state.kind]}</p>
      <p className="max-w-full font-mono text-[11px] break-words text-dock-muted">{state.message}</p>
      {pathMissing && state.fallback && <p className="max-w-full font-mono text-[11px] break-words text-dock-green">{`Repli : ${state.fallback}`}</p>}
      <div className="flex flex-wrap items-center justify-center gap-2">
        {pathMissing ? (
          <>
            <button type="button" className={PRIMARY} data-tip={`Relancer un shell neuf dans ${state.fallback ?? ''}`} onClick={handleRestartInFallback}>
              Relancer dans le dossier de repli
            </button>
            <button ref={defaultRef} type="button" className={SECONDARY} data-overlay-default data-tip="Garder ce shell tel quel et masquer ce message (Entrée)" onClick={onDismiss}>
              Ignorer
            </button>
          </>
        ) : (
          <>
            <button ref={defaultRef} type="button" className={PRIMARY} data-overlay-default data-tip="Relancer le même shell dans ce pane (Entrée)" onClick={onRestart}>
              Relancer
            </button>
            <div className="relative">
              <button ref={shellButtonRef} type="button" className={SECONDARY} aria-haspopup="menu" aria-expanded={menuOpen} data-tip="Relancer ce pane avec un autre shell" onClick={handleOpenMenu}>
                Choisir un shell
              </button>
              {menuOpen && <ShellMenu shells={shells} onSelect={handleSelectShell} onClose={handleCloseMenu} />}
            </div>
          </>
        )}
        <button type="button" className={DANGER} data-tip="Fermer ce pane" onClick={onClose}>
          Fermer le pane
        </button>
      </div>
    </div>
  )
}
