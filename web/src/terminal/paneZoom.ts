import { activeTab, activeWorkspace, panesOf, type Tab } from '../model/session'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { useUiStore } from '../store/uiStore'

const SINGLE_PANE_STATUS = 'Un seul pane dans cet onglet : rien à agrandir.'
const MIN_PANES_TO_ZOOM = 2

const currentTab = (): Tab | undefined => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  return workspace ? activeTab(workspace) : undefined
}

export const zoomedPaneOf = (tab: Tab, zoomedPaneId: string | null) =>
  zoomedPaneId === tab.active && panesOf(tab.tree).length >= MIN_PANES_TO_ZOOM ? panesOf(tab.tree).find((pane) => pane.id === zoomedPaneId) : undefined

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
}
