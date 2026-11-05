import { useCallback, useEffect, useRef, useState, type FocusEvent, type KeyboardEvent } from 'react'
import type { ShellProfile } from '../../../bridge/messages'
import { PaneStateKind, type PaneState } from '../paneStore'
import { ShellMenu } from './ShellMenu'
import { BUTTON_PRIMARY, BUTTON_SECONDARY } from '../../../components/buttonStyles'

interface PaneOverlayProps {
  state: PaneState
  active: boolean
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
const ACTIVATION_GUARD_MS = 600
const ACTIVATION_KEYS = new Set(['Enter', ' '])

const DANGER = `${BUTTON_SECONDARY} text-tily-muted hover:text-tily-error`

export function PaneOverlay({ state, active, shells, onRestart, onRestartIn, onChangeShell, onDismiss, onClose }: PaneOverlayProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const overlayRef = useRef<HTMLDivElement>(null)
  const defaultRef = useRef<HTMLButtonElement>(null)
  const shellButtonRef = useRef<HTMLButtonElement>(null)
  const guardUntilRef = useRef(0)
  const pathMissing = state.kind === PaneStateKind.PathMissing

  useEffect(() => {
    const focused = document.activeElement
    const pane = overlayRef.current?.closest(PANE_SELECTOR)
    const typingInTerminal = Boolean(focused && pane?.contains(focused) && !overlayRef.current?.contains(focused))
    if ((active && focused === document.body) || (focused && pane?.contains(focused))) {
      if (typingInTerminal) {
        guardUntilRef.current = performance.now() + ACTIVATION_GUARD_MS
      }
      defaultRef.current?.focus()
    }
  }, [pathMissing, active])

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
  const ignoreHastyActivation = (event: KeyboardEvent<HTMLDivElement>) => {
    if (ACTIVATION_KEYS.has(event.key) && performance.now() < guardUntilRef.current) {
      event.preventDefault()
    }
  }
  const handleOverlayFocus = (event: FocusEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget && !menuOpen) {
      defaultRef.current?.focus()
    }
  }
  return (
    <div ref={overlayRef} role="alert" tabIndex={-1} className="absolute inset-0 z-10 flex flex-col items-center justify-center gap-3 bg-tily-terminal/90 p-4 text-center" onFocus={handleOverlayFocus} onKeyDownCapture={ignoreHastyActivation} onKeyUpCapture={ignoreHastyActivation}>
      <p className="text-tily-base font-semibold text-tily-ink">{TITLES[state.kind]}</p>
      <p className="max-w-full text-tily-xs break-words text-tily-muted">{state.message}</p>
      {pathMissing && state.fallback && <p className="max-w-full text-tily-xs break-words text-tily-green">{`Repli : ${state.fallback}`}</p>}
      <div className="flex flex-wrap items-center justify-center gap-2">
        {pathMissing ? (
          <>
            <button type="button" className={BUTTON_PRIMARY} data-tip={`Relancer un shell neuf dans ${state.fallback ?? ''}`} onClick={handleRestartInFallback}>
              Relancer dans le dossier de repli
            </button>
            <button ref={defaultRef} type="button" className={BUTTON_SECONDARY} data-overlay-default data-tip="Garder ce shell (Entrée)" onClick={onDismiss}>
              Ignorer
            </button>
          </>
        ) : (
          <>
            <button ref={defaultRef} type="button" className={BUTTON_PRIMARY} data-overlay-default data-tip="Relancer le shell (Entrée)" onClick={onRestart}>
              Relancer
            </button>
            <div className="relative">
              <button ref={shellButtonRef} type="button" className={BUTTON_SECONDARY} aria-haspopup="menu" aria-expanded={menuOpen} data-tip="Relancer avec un autre shell" onClick={handleOpenMenu}>
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
