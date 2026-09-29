import type { PointerEvent as ReactPointerEvent } from 'react'
import { useUiStore, type WorkspaceDropTarget } from '../store/uiStore'
import { trackPointerDrag } from './pointerDrag'

const LIST_SELECTOR = '[data-workspace-list]'
const SLOT_SELECTOR = '[data-workspace-slot]'
const HALF = 2

export type MoveWorkspaceHandler = (workspaceId: string, beforeWorkspaceId?: string) => void

const dropTargetAt = (x: number, y: number): WorkspaceDropTarget | null => {
  const list = document.elementFromPoint(x, y)?.closest<HTMLElement>(LIST_SELECTOR)
  if (!list) {
    return null
  }
  const before = Array.from(list.querySelectorAll<HTMLElement>(SLOT_SELECTOR)).find((slot) => {
    const { top, height } = slot.getBoundingClientRect()
    return y < top + height / HALF
  })
  return { beforeWorkspaceId: before?.dataset.workspaceSlot }
}

const sameDropTarget = (left: WorkspaceDropTarget | null, right: WorkspaceDropTarget | null): boolean =>
  left === right || (left !== null && right !== null && left.beforeWorkspaceId === right.beforeWorkspaceId)

export const isWorkspaceDropTarget = (target: WorkspaceDropTarget | null, beforeWorkspaceId?: string): boolean =>
  target !== null && target.beforeWorkspaceId === beforeWorkspaceId

export const beginWorkspaceDrag = (event: ReactPointerEvent<HTMLElement>, workspaceId: string, onMove: MoveWorkspaceHandler): void =>
  trackPointerDrag(
    event,
    {
      start: () => useUiStore.getState().startDraggingWorkspace(workspaceId),
      move: (x, y) => {
        const target = dropTargetAt(x, y)
        const { workspaceDropTarget, setWorkspaceDropTarget } = useUiStore.getState()
        if (!sameDropTarget(workspaceDropTarget, target)) {
          setWorkspaceDropTarget(target)
        }
      },
      end: (dropped) => {
        const { workspaceDropTarget, stopDraggingWorkspace } = useUiStore.getState()
        stopDraggingWorkspace()
        if (dropped && workspaceDropTarget) {
          onMove(workspaceId, workspaceDropTarget.beforeWorkspaceId)
        }
      },
    },
    { swallowClick: true },
  )
