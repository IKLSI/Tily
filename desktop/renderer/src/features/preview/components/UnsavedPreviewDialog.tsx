import { useEffect, useRef, type PointerEvent } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { folderName } from '../../../model/session'
import { cancelPendingAction, discardBeforePendingAction, saveBeforePendingAction } from '../previewEdit'
import { usePreviewStore } from '../previewStore'
import { keepTabInside } from '../../../components/focusTrap'
import { BUTTON_DANGER, BUTTON_PRIMARY, BUTTON_SECONDARY } from '../../../components/buttonStyles'


const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
  if (event.target === event.currentTarget) {
    cancelPendingAction()
  }
}

export function UnsavedPreviewDialog() {
  const { path, saving } = usePreviewStore(useShallow((store) => ({ path: store.path, saving: store.edit?.saving ?? false })))
  const saveRef = useRef<HTMLButtonElement>(null)
  const title = `Enregistrer « ${path ? folderName(path) : ''} » ?`

  useEffect(() => {
    saveRef.current?.focus()
    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        cancelPendingAction()
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => document.removeEventListener('keydown', handleDocumentKeyDown)
  }, [])

  return (
    <div className="absolute inset-0 z-40 flex items-start justify-center bg-black/30 backdrop-blur-[3px] pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="alertdialog" aria-label={title} className="flex w-[480px] max-w-[94vw] flex-col rounded-tily-lg border border-tily-line bg-tily-raised shadow-tily-dialog" onKeyDown={keepTabInside}>
        <div className="flex items-center justify-between gap-3 border-b border-tily-line px-4 py-3">
          <h2 className="min-w-0 truncate text-tily-lg font-semibold text-tily-ink">{title}</h2>
        </div>
        <div className="flex flex-col gap-2 px-4 py-4">
          <p className="text-tily-sm text-tily-ink">Le fichier contient des modifications non enregistrées.</p>
          <p className="truncate text-tily-xs text-tily-muted">{path}</p>
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-tily-line px-4 py-3">
          <button type="button" className={BUTTON_SECONDARY} data-tip="Échap" onClick={cancelPendingAction}>
            Annuler
          </button>
          <button type="button" className={BUTTON_DANGER} onClick={discardBeforePendingAction}>
            Abandonner les modifications
          </button>
          <button ref={saveRef} type="button" className={BUTTON_PRIMARY} aria-disabled={saving} onClick={saveBeforePendingAction}>
            {saving ? 'Enregistrement…' : 'Enregistrer'}
          </button>
        </div>
      </div>
    </div>
  )
}
