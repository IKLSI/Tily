import { Direction, paneInDirection } from '../components/paneNavigation'
import { activeTab, activeWorkspace, type Tab } from '../model/session'
import { useSessionStore } from '../store/sessionStore'
import { useUiStore } from '../store/uiStore'
import { endPaneZoom } from '../terminal/paneZoom'
import { focusPane } from '../terminal/terminalActions'

const currentTab = (): Tab | undefined => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  return workspace ? activeTab(workspace) : undefined
}

const currentPaneId = (): string => currentTab()?.active ?? ''

const towardPane = (direction: Direction, act: (target: string) => void): void => {
  if (useUiStore.getState().zoomedPaneId !== null) {
    endPaneZoom(true)
    requestAnimationFrame(() => towardPane(direction, act))
    return
  }
  const target = paneInDirection(currentPaneId(), direction)
  if (target) {
    act(target)
  }
}

export const focusPaneToward = (direction: Direction): void => towardPane(direction, (target) => useSessionStore.getState().selectPane(target))

export const swapPaneToward = (direction: Direction): void =>
  towardPane(direction, (target) => {
    useSessionStore.getState().swapActivePane(target)
    requestAnimationFrame(() => focusPane(currentPaneId()))
  })

export const equalizeActiveTab = (): void => {
  const tab = currentTab()
  if (tab) {
    useSessionStore.getState().equalizeSplits(tab.id)
  }
}
