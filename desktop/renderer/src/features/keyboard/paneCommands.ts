import { Direction, paneInDirection } from '../terminal/components/paneNavigation'
import { activeTab, activeWorkspace, type Tab } from '../../model/session'
import { useHostStore } from '../../stores/hostStore'
import { useSessionStore } from '../../stores/sessionStore'
import { useUiStore } from '../../stores/uiStore'
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

const LAST_TAB_NUMBER = 9

export const selectTabNumber = (number: number): void => {
  const { session, selectTab } = useSessionStore.getState()
  const tabs = session ? (activeWorkspace(session)?.tabs ?? []) : []
  const tab = number === LAST_TAB_NUMBER ? tabs.at(-1) : tabs[number - 1]
  if (tab) {
    selectTab(tab.id)
  } else {
    useHostStore.getState().setStatus(`Pas d’onglet ${number} dans ce workspace.`)
  }
}

export const equalizeActiveTab = (): void => {
  const tab = currentTab()
  if (tab) {
    useSessionStore.getState().equalizeSplits(tab.id)
  }
}
