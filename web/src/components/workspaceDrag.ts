import type { PointerEvent as ReactPointerEvent } from 'react'
import { useUiStore, type WorkspaceDropTarget } from '../store/uiStore'

const DRAG_THRESHOLD_PX = 4
const PRIMARY_BUTTON = 0
const SLOT_SELECTOR = '[data-workspace-slot]'
const GRABBING_CURSOR = 'grabbing'

export type MoveWorkspaceHandler = (workspaceId: string, beforeWorkspaceId?: string) => void

const dropTargetAt = (x: number, y: number): WorkspaceDropTarget | null => {
  const slot = document.elementFromPoint(x, y)?.closest<HTMLElement>(SLOT_SELECTOR)
  return slot ? { beforeWorkspaceId: slot.dataset.workspaceSlot || undefined } : null
}

const sameDropTarget = (left: WorkspaceDropTarget | null, right: WorkspaceDropTarget | null): boolean =>
  left === right || (left !== null && right !== null && left.beforeWorkspaceId === right.beforeWorkspaceId)

export const isWorkspaceDropTarget = (target: WorkspaceDropTarget | null, beforeWorkspaceId?: string): boolean =>
  target !== null && target.beforeWorkspaceId === beforeWorkspaceId

export const beginWorkspaceDrag = (event: ReactPointerEvent<HTMLElement>, workspaceId: string, onMove: MoveWorkspaceHandler): void => {
  if (event.button !== PRIMARY_BUTTON) {
    return
  }
  const source = event.currentTarget
  const { pointerId, clientX: startX, clientY: startY } = event
  let dragging = false

  const handleMove = (move: PointerEvent) => {
    if (!dragging) {
      if (Math.abs(move.clientX - startX) < DRAG_THRESHOLD_PX && Math.abs(move.clientY - startY) < DRAG_THRESHOLD_PX) {
        return
      }
      dragging = true
      source.setPointerCapture(pointerId)
      document.body.style.cursor = GRABBING_CURSOR
      useUiStore.getState().startDraggingWorkspace(workspaceId)
    }
    const target = dropTargetAt(move.clientX, move.clientY)
    const { workspaceDropTarget, setWorkspaceDropTarget } = useUiStore.getState()
    if (!sameDropTarget(workspaceDropTarget, target)) {
      setWorkspaceDropTarget(target)
    }
  }
  const handleEnd = () => {
    source.removeEventListener('pointermove', handleMove)
    source.removeEventListener('pointerup', handleEnd)
    source.removeEventListener('pointercancel', handleEnd)
    if (!dragging) {
      return
    }
    const { workspaceDropTarget, stopDraggingWorkspace } = useUiStore.getState()
    document.body.style.cursor = ''
    stopDraggingWorkspace()
    if (workspaceDropTarget) {
      onMove(workspaceId, workspaceDropTarget.beforeWorkspaceId)
    }
  }
  source.addEventListener('pointermove', handleMove)
  source.addEventListener('pointerup', handleEnd)
  source.addEventListener('pointercancel', handleEnd)
}
