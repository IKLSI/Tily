import { activeTab, activeWorkspace, panesOf, type Tab } from '../model/session'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { useUiStore } from '../store/uiStore'

const SINGLE_PANE_STATUS = 'Un seul pane dans cet onglet : rien à agrandir.'
const ZOOMED_STATUS = 'Pane agrandi, les autres tournent toujours : Ctrl + Maj + M ou « Réduire » dans son en-tête pour les revoir.'
const UNZOOMED_STATUS = 'Tous les panes de l’onglet sont de nouveau affichés.'
const MIN_PANES_TO_ZOOM = 2

const currentTab = (): Tab | undefined => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  return workspace ? activeTab(workspace) : undefined
}

export const zoomedPaneOf = (tab: Tab, zoomedPaneId: string | null) =>
  zoomedPaneId === tab.active && panesOf(tab.tree).length >= MIN_PANES_TO_ZOOM ? panesOf(tab.tree).find((pane) => pane.id === zoomedPaneId) : undefined

export const endPaneZoom = (): void => {
  useUiStore.getState().clearPaneZoom()
  const { status, setStatus } = useHostStore.getState()
  if (status.text === ZOOMED_STATUS) {
    setStatus(UNZOOMED_STATUS)
  }
}

export const togglePaneZoom = (paneId?: string): void => {
  const tab = currentTab()
  if (!tab) {
    return
  }
  if (panesOf(tab.tree).length < MIN_PANES_TO_ZOOM) {
    useHostStore.getState().setStatus(SINGLE_PANE_STATUS)
    return
  }
  const target = paneId ?? tab.active
  useSessionStore.getState().selectPane(target)
  useUiStore.getState().togglePaneZoom(target)
  useHostStore.getState().setStatus(useUiStore.getState().zoomedPaneId === target ? ZOOMED_STATUS : UNZOOMED_STATUS)
}
