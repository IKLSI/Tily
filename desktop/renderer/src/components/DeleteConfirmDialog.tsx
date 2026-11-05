import { useEffect, useRef, type PointerEvent } from 'react'
import type { DeleteRequest } from '../features/explorer/explorerStore'
import { keepTabInside } from './focusTrap'
import { BUTTON_DANGER, BUTTON_SECONDARY } from './buttonStyles'

interface DeleteConfirmDialogProps {
  request: DeleteRequest
  onConfirm: (request: DeleteRequest) => void
  onCancel: () => void
}


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
    <div className="absolute inset-0 z-40 flex items-start justify-center bg-black/30 backdrop-blur-[3px] pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="alertdialog" aria-label={title} className="flex w-[480px] max-w-[94vw] flex-col rounded-tily-lg border border-tily-line bg-tily-raised shadow-tily-dialog" onKeyDown={keepTabInside}>
        <div className="flex items-center justify-between gap-3 border-b border-tily-line px-4 py-3">
          <h2 className="min-w-0 truncate text-tily-lg font-semibold text-tily-ink">{title}</h2>
        </div>
        <div className="flex flex-col gap-2 px-4 py-4">
          <p className="text-tily-sm text-tily-ink">
            {request.isDirectory ? 'Le dossier et tout son contenu seront placés dans la Corbeille.' : 'Le fichier sera placé dans la Corbeille.'}
          </p>
          <p className="truncate text-tily-xs text-tily-muted">{request.path}</p>
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-tily-line px-4 py-3">
          <button type="button" className={BUTTON_SECONDARY} data-tip="Échap" onClick={onCancel}>
            Annuler
          </button>
          <button ref={confirmRef} type="button" className={BUTTON_DANGER} data-tip="Entrée" onClick={handleConfirm}>
            Placer dans la Corbeille
          </button>
        </div>
      </div>
    </div>
  )
}
