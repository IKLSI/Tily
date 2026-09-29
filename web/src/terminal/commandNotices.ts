import { activeTab, activeWorkspace, allPanes, panesOf, type Tab } from '../model/session'
import { useCommandStore, type CommandNotice } from '../store/commandStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'

export const COMMAND_DONE_OSC = 6973
const DONE_KIND = 'done'
const SUCCESS_FLAG = '1'
const NOTICE_MIN_MS = 10_000
const SECOND_MS = 1000
const SECONDS_PER_MINUTE = 60
const MINUTES_PER_HOUR = 60

export const formatCommandDuration = (durationMs: number): string => {
  const seconds = Math.round(durationMs / SECOND_MS)
  const minutes = Math.floor(seconds / SECONDS_PER_MINUTE)
  if (minutes < 1) {
    return `${seconds} s`
  }
  if (minutes < MINUTES_PER_HOUR) {
    return `${minutes} min ${String(seconds % SECONDS_PER_MINUTE).padStart(2, '0')} s`
  }
  return `${Math.floor(minutes / MINUTES_PER_HOUR)} h ${String(minutes % MINUTES_PER_HOUR).padStart(2, '0')} min`
}

export const commandNoticeTip = (notice: CommandNotice): string =>
  `${notice.success ? 'Commande terminée' : 'Commande en échec'} après ${formatCommandDuration(notice.durationMs)}`

const parseCommandDone = (data: string): CommandNotice | null => {
  const [kind, duration, success] = data.split(';')
  const durationMs = Number(duration)
  return kind === DONE_KIND && duration !== '' && Number.isFinite(durationMs) ? { durationMs, success: success === SUCCESS_FLAG } : null
}

const displayedTab = (): Tab | undefined => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  return workspace ? activeTab(workspace) : undefined
}

const tabOfPane = (paneId: string): Tab | undefined =>
  useSessionStore
    .getState()
    .session?.workspaces.flatMap((workspace) => workspace.tabs)
    .find((tab) => panesOf(tab.tree).some((pane) => pane.id === paneId))

export const tabCommandNotice = (tab: Tab, notices: Record<string, CommandNotice>): CommandNotice | undefined => {
  const tabNotices = panesOf(tab.tree).flatMap((pane) => notices[pane.id] ?? [])
  return tabNotices.find((notice) => !notice.success) ?? tabNotices[0]
}

export const receiveCommandDone = (paneId: string, data: string): void => {
  const notice = parseCommandDone(data)
  const tab = tabOfPane(paneId)
  if (!notice || !tab || notice.durationMs < NOTICE_MIN_MS || displayedTab()?.id === tab.id) {
    return
  }
  useCommandStore.getState().notify(paneId, notice)
  useHostStore.getState().setStatus(`${commandNoticeTip(notice)} dans l’onglet « ${tab.name} ».`, notice.success ? StatusLevel.Info : StatusLevel.Error)
}

export const clearSeenCommandNotices = (): void => {
  const { notices, keepOnly } = useCommandStore.getState()
  const { session } = useSessionStore.getState()
  if (Object.keys(notices).length === 0 || !session) {
    return
  }
  const shownTab = displayedTab()
  const shown = new Set(shownTab ? panesOf(shownTab.tree).map((pane) => pane.id) : [])
  const alive = new Set(allPanes(session).map((pane) => pane.id))
  keepOnly((paneId) => alive.has(paneId) && !shown.has(paneId))
}
