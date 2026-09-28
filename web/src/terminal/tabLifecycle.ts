import { findWorkspace, panesOf, type Tab } from '../model/session'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { requestClose } from './closeGuard'
import { terminalRegistry } from './terminalRegistry'
import { keepClosedTabText, takeClosedTabText } from './textPersistence'

const tabOf = (tabId: string) =>
  useSessionStore
    .getState()
    .session?.workspaces.flatMap((workspace) => workspace.tabs)
    .find((tab) => tab.id === tabId)

const paneIdsOf = (tab: Tab): string[] => panesOf(tab.tree).map((pane) => pane.id)

const closeTabNow = (tabId: string): void => {
  const tab = tabOf(tabId)
  if (!tab) {
    return
  }
  const text = terminalRegistry.snapshot(paneIdsOf(tab))
  useSessionStore.getState().closeTab(tabId)
  keepClosedTabText(text)
  useHostStore.getState().setStatus('Onglet fermé. Ctrl + Maj + Z le rouvre avec un nouveau terminal.')
}

export const closeTabKeepingText = (tabId: string): void => {
  const tab = tabOf(tabId)
  if (tab) {
    requestClose(`Fermer l’onglet « ${tab.name} » ?`, paneIdsOf(tab), () => closeTabNow(tabId))
  }
}

const closeTabsNow = (tabIds: string[]): void => {
  const tabs = tabIds.map(tabOf).filter((tab): tab is Tab => tab !== undefined)
  const text = terminalRegistry.snapshot(tabs.flatMap(paneIdsOf))
  const { closeTab } = useSessionStore.getState()
  for (const tab of tabs) {
    closeTab(tab.id)
  }
  keepClosedTabText(text)
  useHostStore.getState().setStatus(tabs.length === 1 ? 'Onglet fermé. Ctrl + Maj + Z le rouvre avec un nouveau terminal.' : `${tabs.length} onglets fermés. Ctrl + Maj + Z rouvre les derniers un par un.`)
}

export const closeOtherTabsKeepingText = (tabId: string): void => {
  const { session } = useSessionStore.getState()
  const others = session?.workspaces.find((workspace) => workspace.tabs.some((tab) => tab.id === tabId))?.tabs.filter((tab) => tab.id !== tabId) ?? []
  if (others.length > 0) {
    const title = others.length === 1 ? 'Fermer l’autre onglet ?' : `Fermer les ${others.length} autres onglets ?`
    requestClose(title, others.flatMap(paneIdsOf), () => closeTabsNow(others.map((tab) => tab.id)))
  }
}

const closeWorkspaceNow = (workspaceId: string): void => {
  const { session, closeTab } = useSessionStore.getState()
  const workspace = session ? findWorkspace(session, workspaceId) : undefined
  if (!workspace) {
    return
  }
  const text = terminalRegistry.snapshot(workspace.tabs.flatMap(paneIdsOf))
  for (const tab of workspace.tabs) {
    closeTab(tab.id)
  }
  keepClosedTabText(text)
  useHostStore.getState().setStatus(`Workspace « ${workspace.name} » fermé. Ctrl + Maj + Z rouvre ses derniers onglets un par un.`)
}

export const closeWorkspaceKeepingText = (workspaceId: string): void => {
  const { session } = useSessionStore.getState()
  const workspace = session ? findWorkspace(session, workspaceId) : undefined
  if (workspace) {
    requestClose(`Fermer le workspace « ${workspace.name} » ?`, workspace.tabs.flatMap(paneIdsOf), () => closeWorkspaceNow(workspaceId))
  }
}

export const closePaneKeepingText = (paneId: string): void => {
  const { session, closePane } = useSessionStore.getState()
  const tab = session?.workspaces.flatMap((workspace) => workspace.tabs).find((candidate) => panesOf(candidate.tree).some((pane) => pane.id === paneId))
  if (!tab) {
    return
  }
  if (panesOf(tab.tree).length === 1) {
    closeTabKeepingText(tab.id)
  } else {
    requestClose('Fermer le pane ?', [paneId], () => closePane(paneId))
  }
}

export const duplicateTabKeepingLayout = (tabId: string): void => {
  const tab = tabOf(tabId)
  if (tab) {
    useSessionStore.getState().duplicateTab(tabId)
    useHostStore.getState().setStatus(`Onglet « ${tab.name} » dupliqué : nouveaux terminaux dans les mêmes dossiers.`)
  }
}

export const restoreClosedTab = (): void => {
  const restored = useSessionStore.getState().restoreTab()
  if (!restored) {
    useHostStore.getState().setStatus('Aucun onglet fermé à rouvrir.')
    return
  }
  for (const [paneId, text] of Object.entries(takeClosedTabText(restored.paneIds))) {
    terminalRegistry.prime(paneId, text)
  }
  useHostStore.getState().setStatus(`Onglet « ${restored.tab.name} » rouvert avec de nouveaux terminaux.`)
}
