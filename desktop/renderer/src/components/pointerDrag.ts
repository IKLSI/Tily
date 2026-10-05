import type { PointerEvent as ReactPointerEvent } from 'react'

const DRAG_THRESHOLD_PX = 4
const PRIMARY_BUTTON = 0
const NO_BUTTONS = 0
const GRABBING_CURSOR = 'grabbing'
const EDITABLE_SELECTOR = 'input, textarea, select, [contenteditable="true"]'

interface PointerDragHandlers {
  start: () => void
  move: (x: number, y: number) => void
  end: (dropped: boolean) => void
}

interface PointerDragOptions {
  swallowClick?: boolean
}

const startsInEditable = (target: EventTarget): boolean => target instanceof Element && target.closest(EDITABLE_SELECTOR) !== null

const swallowNextClick = (): void => {
  const swallow = (click: MouseEvent) => {
    click.stopPropagation()
    click.preventDefault()
  }
  window.addEventListener('click', swallow, { capture: true, once: true })
  setTimeout(() => window.removeEventListener('click', swallow, { capture: true }))
}

export const trackPointerDrag = (event: ReactPointerEvent<HTMLElement>, handlers: PointerDragHandlers, options: PointerDragOptions = {}): void => {
  if (event.button !== PRIMARY_BUTTON || startsInEditable(event.target)) {
    return
  }
  const source = event.currentTarget
  const { pointerId, clientX: startX, clientY: startY } = event
  const listeners = new AbortController()
  let dragging = false

  const finish = (dropped: boolean) => {
    listeners.abort()
    if (!dragging) {
      return
    }
    document.body.style.cursor = ''
    if (options.swallowClick) {
      swallowNextClick()
    }
    handlers.end(dropped)
  }
  const handleMove = (move: PointerEvent) => {
    if (move.pointerId !== pointerId) {
      return
    }
    if (move.buttons === NO_BUTTONS) {
      finish(false)
      return
    }
    if (!dragging) {
      if (Math.abs(move.clientX - startX) < DRAG_THRESHOLD_PX && Math.abs(move.clientY - startY) < DRAG_THRESHOLD_PX) {
        return
      }
      dragging = true
      source.setPointerCapture(pointerId)
      document.body.style.cursor = GRABBING_CURSOR
      handlers.start()
    }
    handlers.move(move.clientX, move.clientY)
  }
  const handleUp = (up: PointerEvent) => {
    if (up.pointerId === pointerId) {
      finish(true)
    }
  }
  const handleCancel = (cancel: PointerEvent) => {
    if (cancel.pointerId === pointerId) {
      finish(false)
    }
  }
  window.addEventListener('pointermove', handleMove, { signal: listeners.signal })
  window.addEventListener('pointerup', handleUp, { signal: listeners.signal })
  window.addEventListener('pointercancel', handleCancel, { signal: listeners.signal })
}
