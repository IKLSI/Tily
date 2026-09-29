import { useEffect, useRef, type PointerEvent } from 'react'
import type { DeleteRequest } from '../store/explorerStore'
import { keepTabInside } from './focusTrap'

interface DeleteConfirmDialogProps {
  request: DeleteRequest
  onConfirm: (request: DeleteRequest) => void
  onCancel: () => void
}

const BUTTON = 'cursor-pointer rounded border px-3 py-1.5 text-[12px]'
const DANGER = `${BUTTON} border-dock-error text-dock-error hover:bg-dock-green-hover`
const SECONDARY = `${BUTTON} border-dock-line text-dock-ink hover:bg-dock-green-hover`

export function DeleteConfirmDialog({ request, onConfirm, onCancel }: DeleteConfirmDialogProps) {
  const confirmRef = useRef<HTMLButtonElement>(null)
  const title = `Supprimer « ${request.name} » ?`

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
  const handleConfirm = () => onConfirm(request)

  return (
    <div className="absolute inset-0 z-40 flex items-start justify-center bg-dock-paper/60 pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="alertdialog" aria-label={title} className="flex w-[480px] max-w-[94vw] flex-col rounded-lg border border-dock-line bg-dock-panel shadow-xl" onKeyDown={keepTabInside}>
        <div className="flex items-center justify-between gap-3 border-b border-dock-line px-4 py-3">
          <h2 className="min-w-0 truncate text-[15px] font-semibold text-dock-ink">{title}</h2>
          <span className="shrink-0 text-[11px] text-dock-muted">Entrée supprime · Échap annule</span>
        </div>
        <div className="flex flex-col gap-2 px-4 py-4">
          <p className="text-[12px] text-dock-ink">
            {request.isDirectory ? 'Le dossier et tout son contenu seront placés dans la corbeille de Windows.' : 'Le fichier sera placé dans la corbeille de Windows.'}
          </p>
          <p className="truncate font-mono text-[11px] text-dock-muted">{request.path}</p>
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-dock-line px-4 py-3">
          <button type="button" className={SECONDARY} onClick={onCancel}>
            Annuler
          </button>
          <button ref={confirmRef} type="button" className={DANGER} onClick={handleConfirm}>
            Placer dans la corbeille
          </button>
        </div>
      </div>
    </div>
  )
}
