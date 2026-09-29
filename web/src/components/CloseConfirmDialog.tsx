import { useEffect, useRef, type PointerEvent } from 'react'
import type { CloseConfirmation } from '../store/uiStore'
import { keepTabInside } from './focusTrap'

interface CloseConfirmDialogProps {
  confirmation: CloseConfirmation
  onConfirm: () => void
  onCancel: () => void
}

const BUTTON = 'cursor-pointer rounded border px-3 py-1.5 text-[12px]'
const DANGER = `${BUTTON} border-dock-error text-dock-error hover:bg-dock-green-hover`
const SECONDARY = `${BUTTON} border-dock-line text-dock-ink hover:bg-dock-green-hover`

export function CloseConfirmDialog({ confirmation, onConfirm, onCancel }: CloseConfirmDialogProps) {
  const confirmRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    confirmRef.current?.focus()
    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        onCancel()
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => document.removeEventListener('keydown', handleDocumentKeyDown)
  }, [onCancel])

  const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onCancel()
    }
  }

  return (
    <div className="absolute inset-0 z-40 flex items-start justify-center bg-dock-paper/60 pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="alertdialog" aria-label={confirmation.title} className="flex max-h-[76vh] w-[520px] max-w-[94vw] flex-col rounded-lg border border-dock-line bg-dock-panel shadow-xl" onKeyDown={keepTabInside}>
        <div className="flex items-center justify-between border-b border-dock-line px-4 py-3">
          <h2 className="text-[15px] font-semibold text-dock-ink">{confirmation.title}</h2>
          <span className="text-[11px] text-dock-muted">Entrée arrête · Échap annule</span>
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4 py-4">
          <p className="text-[12px] text-dock-warning">Des programmes tournent encore. Ils seront arrêtés de force avec tout leur arbre de processus.</p>
          <ul className="flex flex-col gap-2">
            {confirmation.panes.map((pane) => (
              <li key={pane.paneId} className="rounded border border-dock-line bg-dock-paper px-3 py-2">
                <div className="truncate text-[12px] text-dock-ink">{pane.label}</div>
                <div className="font-mono text-[11px] text-dock-muted">{pane.processes.join(', ')}</div>
              </li>
            ))}
          </ul>
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-dock-line px-4 py-3">
          <button type="button" className={SECONDARY} onClick={onCancel}>
            Annuler
          </button>
          <button ref={confirmRef} type="button" className={DANGER} onClick={onConfirm}>
            {confirmation.confirmLabel ?? 'Arrêter et fermer'}
          </button>
        </div>
      </div>
    </div>
  )
}
