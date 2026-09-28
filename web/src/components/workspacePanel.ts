import type { KeyboardEvent } from 'react'
import type { Session } from '../model/session'
import type { MoveTabHandler } from './tabDrag'

export interface WorkspacePanelActions {
  selectWorkspace: (workspaceId: string) => void
  toggleWorkspace: (workspaceId: string) => void
  startRenameWorkspace: (workspaceId: string) => void
  commitRenameWorkspace: (name: string) => void
  cancelRenameWorkspace: () => void
  closeWorkspace: (workspaceId: string) => void
  newTabIn: (workspaceId: string) => void
  collapseOthers: (workspaceId: string) => void
  moveWorkspace: (workspaceId: string, offset: number) => void
  shiftTab: (tabId: string, offset: number) => void
  selectTab: (workspaceId: string, tabId: string) => void
  startRenameTab: (tabId: string) => void
  commitRenameTab: (name: string) => void
  cancelRenameTab: () => void
  closeTab: (tabId: string) => void
  moveTab: MoveTabHandler
  joinPane: (paneId: string) => void
  newWorkspace: () => void
  openProjects: () => void
}

export interface PanelMenuRequest {
  workspaceId: string
  tabId?: string
  x: number
  y: number
  returnFocus: HTMLElement | null
}

export const PANEL_CLOSE_BUTTON =
  'flex size-[20px] shrink-0 cursor-pointer items-center justify-center rounded text-dock-muted opacity-0 group-hover:opacity-100 focus-visible:opacity-100 hover:bg-dock-green-hover hover:text-dock-error'

export const PANEL_DROP_LINE =
  'pointer-events-none absolute -top-px right-0 left-0 h-[2px] rounded-full bg-dock-focus before:absolute before:-top-[2px] before:-left-[3px] before:size-[6px] before:rounded-full before:bg-dock-focus'

export interface MenuPlace {
  position: number
  count: number
}

export const MOVE_KEYS: Record<string, number> = { ArrowUp: -1, ArrowDown: 1 }

export const menuPlaceOf = (session: Session, { workspaceId, tabId }: PanelMenuRequest): MenuPlace => {
  const tabs = session.workspaces.find((workspace) => workspace.id === workspaceId)?.tabs ?? []
  return tabId
    ? { position: tabs.findIndex((tab) => tab.id === tabId), count: tabs.length }
    : { position: session.workspaces.findIndex((workspace) => workspace.id === workspaceId), count: session.workspaces.length }
}

export const isMenuKey = (event: KeyboardEvent): boolean => (event.shiftKey && event.key === 'F10') || event.key === 'ContextMenu'

export const menuRequestFor = (event: KeyboardEvent<HTMLElement>, workspaceId: string, tabId?: string): PanelMenuRequest => {
  const { left, bottom } = event.currentTarget.getBoundingClientRect()
  return { workspaceId, tabId, x: left, y: bottom, returnFocus: event.currentTarget }
}
