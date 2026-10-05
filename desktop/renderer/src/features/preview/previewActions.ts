import { bridge } from '../../bridge/bridge'
import type { HostMessageOf } from '../../bridge/messages'
import { activeTab, activeWorkspace, folderName, panesOf, RightPanelView, type Session, type Tab, type Workspace } from '../../model/session'
import { useHostStore } from '../../stores/hostStore'
import { usePreviewStore } from './previewStore'
import { useSessionStore } from '../../stores/sessionStore'
import { guardEdits } from './previewEdit'

const showPreview = (path: string): void => {
  usePreviewStore.getState().open(path)
  bridge.send({ type: 'preview.open', path })
}

export const openPreview = (path: string): void => {
  if (usePreviewStore.getState().path === path) {
    showPreview(path)
  } else {
    guardEdits(() => showPreview(path))
  }
}

export const closePreview = (onClosed?: () => void): void => {
  if (usePreviewStore.getState().path !== null) {
    guardEdits(() => {
      usePreviewStore.getState().close()
      bridge.send({ type: 'preview.close' })
      onClosed?.()
    })
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

export const openPreviewInBrowser = (): void => {
  const { path } = usePreviewStore.getState()
  if (path !== null) {
    bridge.send({ type: 'preview.browser', path })
    useHostStore.getState().setStatus(`Ouverture dans le navigateur : ${folderName(path)}`)
  }
}

const displayedTabId = (session: Session | null): string | null => {
  const workspace = session ? activeWorkspace(session) : undefined
  return workspace ? activeTab(workspace).id : null
}

const locatePane = (session: Session, paneId: string): { workspace: Workspace; tab: Tab } | undefined => {
  for (const workspace of session.workspaces) {
    const tab = workspace.tabs.find((candidate) => panesOf(candidate.tree).some((pane) => pane.id === paneId))
    if (tab) {
      return { workspace, tab }
    }
  }
  return undefined
}

const showInFilesView = (path: string): void => {
  const { session, toggleExplorer, setPanelView } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  if (workspace && !activeTab(workspace).explorer) {
    toggleExplorer()
  }
  setPanelView(RightPanelView.Files)
  openPreview(path)
}

export const receivePreviewRequest = (message: HostMessageOf<'preview.requested'>): void => {
  const { session } = useSessionStore.getState()
  const location = session ? locatePane(session, message.pane) : undefined
  if (!location) {
    return
  }
  const name = folderName(message.path)
  if (displayedTabId(session) === location.tab.id) {
    showInFilesView(message.path)
    useHostStore.getState().setStatus(`Aperçu ouvert par Claude Code : ${name}`)
  } else {
    usePreviewStore.getState().defer(location.tab.id, message.path)
    useHostStore.getState().setStatus(`Aperçu prêt dans ${location.workspace.name} › ${location.tab.name} : ${name}`)
  }
}

export const startDeferredPreviews = (): (() => void) =>
  useSessionStore.subscribe((state, previous) => {
    const tabId = displayedTabId(state.session)
    if (tabId === null || tabId === displayedTabId(previous.session)) {
      return
    }
    const path = usePreviewStore.getState().takeDeferred(tabId)
    if (path !== null) {
      showInFilesView(path)
    }
  })
