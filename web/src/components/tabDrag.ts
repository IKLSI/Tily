import type { PointerEvent as ReactPointerEvent } from 'react'
import { useUiStore, type TabDropTarget } from '../store/uiStore'
import { trackPointerDrag } from './pointerDrag'

const SPRING_DELAY_MS = 600

export type MoveTabHandler = (tabId: string, workspaceId: string, beforeTabId?: string) => void

const dropTargetAt = (x: number, y: number): TabDropTarget | null => {
  const element = document.elementFromPoint(x, y)?.closest<HTMLElement>('[data-drop-workspace]')
  if (!element?.dataset.dropWorkspace) {
    return null
  }
  return { workspaceId: element.dataset.dropWorkspace, beforeTabId: element.dataset.dropTab }
}

const springWorkspaceAt = (x: number, y: number): string | undefined =>
  document.elementFromPoint(x, y)?.closest<HTMLElement>('[data-spring-workspace]')?.dataset.springWorkspace

export const isDropTarget = (target: TabDropTarget | null, workspaceId: string, beforeTabId?: string): boolean =>
  target !== null && target.workspaceId === workspaceId && target.beforeTabId === beforeTabId

const sameDropTarget = (left: TabDropTarget | null, right: TabDropTarget | null): boolean =>
  left === right || (left !== null && right !== null && isDropTarget(left, right.workspaceId, right.beforeTabId))

export const beginTabDrag = (event: ReactPointerEvent<HTMLElement>, tabId: string, onMove: MoveTabHandler): void => {
  let springCandidate: string | undefined
  let springTimer: ReturnType<typeof setTimeout> | undefined

  const followSpringCandidate = (x: number, y: number) => {
    const candidate = springWorkspaceAt(x, y)
    if (candidate === springCandidate) {
      return
    }
    springCandidate = candidate
    clearTimeout(springTimer)
    if (candidate) {
      springTimer = setTimeout(() => useUiStore.getState().openSpringWorkspace(candidate), SPRING_DELAY_MS)
    }
  }
  trackPointerDrag(event, {
    start: () => useUiStore.getState().startDraggingTab(tabId),
    move: (x, y) => {
      const target = dropTargetAt(x, y)
      const { tabDropTarget, setTabDropTarget } = useUiStore.getState()
      if (!sameDropTarget(tabDropTarget, target)) {
        setTabDropTarget(target)
      }
      followSpringCandidate(x, y)
    },
    end: (dropped) => {
      clearTimeout(springTimer)
      const { tabDropTarget, stopDraggingTab } = useUiStore.getState()
      stopDraggingTab()
      if (dropped && tabDropTarget) {
        onMove(tabId, tabDropTarget.workspaceId, tabDropTarget.beforeTabId)
      }
    },
  })
}
