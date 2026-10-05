import { bridge } from '../../bridge/bridge'
import type { HostMessageOf } from '../../bridge/messages'
import { folderName } from '../../model/session'
import { StatusLevel, useHostStore } from '../../stores/hostStore'
import { editDirty, usePreviewStore } from './previewStore'

export const guardEdits = (action: () => void): void => {
  if (editDirty(usePreviewStore.getState().edit)) {
    usePreviewStore.getState().ask(action)
  } else {
    action()
  }
}

export const startPreviewEdit = (): void => usePreviewStore.getState().startEdit()

export const stopPreviewEdit = (): void => guardEdits(() => usePreviewStore.getState().stopEdit())

export const savePreview = (force = false): void => {
  const { path, edit, markSaving } = usePreviewStore.getState()
  if (path === null || !edit || edit.saving || (!force && !editDirty(edit))) {
    return
  }
  markSaving()
  bridge.send({ type: 'preview.save', path, content: edit.draft, version: edit.version, force })
}

export const overwritePreview = (): void => savePreview(true)

export const reloadPreviewFromDisk = (): void => usePreviewStore.getState().reloadFromDisk()

export const receivePreviewSaved = (message: HostMessageOf<'preview.saved'>): void => {
  const store = usePreviewStore.getState()
  store.receiveSave(message.preview, true)
  useHostStore.getState().setStatus(`Fichier enregistré : ${folderName(message.preview.path)}`)
  const { edit, pendingAction } = usePreviewStore.getState()
  if (pendingAction && !editDirty(edit)) {
    usePreviewStore.getState().clearPending()
    pendingAction()
  }
}

export const receivePreviewSaveFailed = (message: HostMessageOf<'preview.saveFailed'>): void => {
  usePreviewStore.getState().receiveSave(message.preview, false)
  usePreviewStore.getState().clearPending()
  useHostStore.getState().setStatus(message.message, StatusLevel.Error)
}

export const saveBeforePendingAction = (): void => savePreview()

export const discardBeforePendingAction = (): void => {
  const { pendingAction, clearPending, stopEdit } = usePreviewStore.getState()
  clearPending()
  stopEdit()
  pendingAction?.()
}

export const cancelPendingAction = (): void => usePreviewStore.getState().clearPending()
