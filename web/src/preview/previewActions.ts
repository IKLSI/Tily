import { bridge } from '../bridge/bridge'
import type { HostMessageOf } from '../bridge/messages'
import { folderName } from '../model/session'
import { useHostStore } from '../store/hostStore'
import { usePreviewStore } from '../store/previewStore'

export const openPreview = (path: string): void => {
  usePreviewStore.getState().open(path)
  bridge.send({ type: 'preview.open', path })
}

export const closePreview = (): void => {
  if (usePreviewStore.getState().path !== null) {
    usePreviewStore.getState().close()
    bridge.send({ type: 'preview.close' })
  }
}

const decodeAnchor = (anchor: string): string => {
  try {
    return decodeURIComponent(anchor)
  } catch {
    return anchor
  }
}

export const followPreviewLink = (href: string): void => {
  const { path, jumpTo } = usePreviewStore.getState()
  if (href.startsWith('#')) {
    jumpTo(decodeAnchor(href.slice(1)))
  } else if (path !== null) {
    bridge.send({ type: 'preview.follow', path, href })
  }
}

export const openPreviewInEditor = (): void => {
  const { path } = usePreviewStore.getState()
  if (path !== null) {
    bridge.send({ type: 'files.open', path })
    useHostStore.getState().setStatus(`Ouverture dans l’éditeur : ${folderName(path)}`)
  }
}

export const receivePreview = (message: HostMessageOf<'preview.loaded'>): void =>
  usePreviewStore.getState().receive(message.preview, message.anchor ?? null, message.reload)
