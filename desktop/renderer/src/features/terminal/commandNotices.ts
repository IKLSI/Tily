import { bridge } from '../../bridge/bridge'
import { activeTab, activeWorkspace, allPanes, panesOf, type Tab, type Workspace } from '../../model/session'
import { useCommandStore, type CommandNotice } from './commandStore'
import { usePaneStore } from './paneStore'
import { StatusLevel, useHostStore } from '../../stores/hostStore'
import { useSessionStore } from '../../stores/sessionStore'

export const COMMAND_DONE_OSC = 6973
const DONE_KIND = 'done'
const SUCCESS_FLAG = '1'
const NOTICE_MIN_MS = 10_000
const SECOND_MS = 1000
const SECONDS_PER_MINUTE = 60
const MINUTES_PER_HOUR = 60
const COMMAND_LABEL_CHARS = 48

export const decodeCommandText = (value: string | undefined): string => {
  try {
    return value ? new TextDecoder().decode(Uint8Array.from(atob(value), (character) => character.charCodeAt(0))) : ''
  } catch {
    return ''
  }
}

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

const commandLabel = (command: string): string => {
  const firstLine = (command.split(/\r?\n/)[0] ?? '').trim()
  return firstLine.length > COMMAND_LABEL_CHARS ? `${firstLine.slice(0, COMMAND_LABEL_CHARS - 1)}…` : firstLine
}

export const commandNoticeTip = (notice: CommandNotice): string => {
  const label = commandLabel(notice.command)
  const outcome = notice.success ? 'terminée' : 'en échec'
  const duration = formatCommandDuration(notice.durationMs)
  return label ? `« ${label} » ${outcome} après ${duration}` : `Commande ${outcome} après ${duration}`
}

export const parseCommandDone = (data: string): CommandNotice | null => {
  const [kind, duration, success, command] = data.split(';')
  const durationMs = Number(duration)
  return kind === DONE_KIND && duration !== '' && Number.isFinite(durationMs) ? { durationMs, success: success === SUCCESS_FLAG, command: decodeCommandText(command) } : null
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

export const workspaceCommandNotice = (workspace: Workspace, notices: Record<string, CommandNotice>): CommandNotice | undefined => {
  const tabNotices = workspace.tabs.flatMap((tab) => tabCommandNotice(tab, notices) ?? [])
  return tabNotices.find((notice) => !notice.success) ?? tabNotices[0]
}

export const receiveCommandDone = (paneId: string, data: string): void => {
  const notice = parseCommandDone(data)
  if (notice) {
    usePaneStore.getState().clearDevServer(paneId)
  }
  const tab = tabOfPane(paneId)
  if (!notice || !tab || notice.durationMs < NOTICE_MIN_MS) {
    return
  }
  bridge.send({ type: 'attention.flash' })
  if (displayedTab()?.id === tab.id) {
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
