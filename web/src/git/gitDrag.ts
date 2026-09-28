import type { PointerEvent as ReactPointerEvent } from 'react'
import type { GitRefKind } from '../bridge/gitMessages'
import { useGitStore, type GitRefHandle } from '../store/gitStore'
import { canDropRef, dropMenu } from './gitMenus'

const DRAG_THRESHOLD_PX = 4
const PRIMARY_BUTTON = 0
const DROP_SELECTOR = '[data-git-drop-name]'

export const sameRef = (left: GitRefHandle | null | undefined, right: GitRefHandle): boolean => left?.kind === right.kind && left.name === right.name

const refAt = (x: number, y: number): GitRefHandle | null => {
  const element = document.elementFromPoint(x, y)?.closest<HTMLElement>(DROP_SELECTOR)
  const kind = element?.dataset.gitDropKind as GitRefKind | undefined
  const name = element?.dataset.gitDropName
  return kind && name ? { kind, name } : null
}

const openDropMenu = (source: GitRefHandle, target: GitRefHandle, x: number, y: number, restoreFocus: () => void): void => {
  const state = useGitStore.getState().state
  if (state) {
    useGitStore.getState().openMenu({ x, y, label: `Déposer « ${source.name} » sur « ${target.name} »`, items: dropMenu(source, target, state), restoreFocus })
  }
}

export const beginRefDrag = (event: ReactPointerEvent<HTMLElement>, source: GitRefHandle, restoreFocus: () => void): void => {
  if (event.button !== PRIMARY_BUTTON) {
    return
  }
  const element = event.currentTarget
  const { pointerId, clientX: startX, clientY: startY } = event
  let dragging = false

  const handleMove = (move: PointerEvent) => {
    if (!dragging) {
      if (Math.abs(move.clientX - startX) < DRAG_THRESHOLD_PX && Math.abs(move.clientY - startY) < DRAG_THRESHOLD_PX) {
        return
      }
      dragging = true
      element.setPointerCapture(pointerId)
      document.documentElement.dataset.gitDragging = 'true'
    }
    const state = useGitStore.getState().state
    const candidate = refAt(move.clientX, move.clientY)
    const target = candidate && state && canDropRef(source, candidate, state) ? candidate : null
    useGitStore.getState().setDrag({ source, x: move.clientX, y: move.clientY, target })
  }
  const handleEnd = (end: PointerEvent) => {
    element.removeEventListener('pointermove', handleMove)
    element.removeEventListener('pointerup', handleEnd)
    element.removeEventListener('pointercancel', handleEnd)
    if (!dragging) {
      return
    }
    delete document.documentElement.dataset.gitDragging
    const target = useGitStore.getState().drag?.target
    useGitStore.getState().setDrag(null)
    if (target && end.type === 'pointerup') {
      openDropMenu(source, target, end.clientX, end.clientY, restoreFocus)
    }
  }
  element.addEventListener('pointermove', handleMove)
  element.addEventListener('pointerup', handleEnd)
  element.addEventListener('pointercancel', handleEnd)
}
