import { useEffect, useRef, type KeyboardEvent, type PointerEvent } from 'react'

const KEYBOARD_STEP = 15

interface SidebarResizerProps {
  width: number
  min: number
  max: number
  label: string
  defaultWidth: number
  reversed?: boolean
  onResize: (width: number) => void
}

export function SidebarResizer({ width, min, max, label, defaultWidth, reversed = false, onResize }: SidebarResizerProps) {
  const dragRef = useRef<{ startX: number; startWidth: number } | null>(null)
  const frameRef = useRef(0)
  const pendingWidthRef = useRef<number | null>(null)
  const direction = reversed ? -1 : 1

  useEffect(() => () => cancelAnimationFrame(frameRef.current), [])

  const flushPendingWidth = () => {
    cancelAnimationFrame(frameRef.current)
    frameRef.current = 0
    const pending = pendingWidthRef.current
    pendingWidthRef.current = null
    if (pending !== null) {
      onResize(pending)
    }
  }

  const handlePointerDown = (event: PointerEvent<HTMLDivElement>) => {
    event.preventDefault()
    event.currentTarget.setPointerCapture(event.pointerId)
    dragRef.current = { startX: event.clientX, startWidth: width }
    document.body.style.cursor = 'ew-resize'
  }
  const handlePointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (!dragRef.current) {
      return
    }
    pendingWidthRef.current = dragRef.current.startWidth + direction * (event.clientX - dragRef.current.startX)
    if (frameRef.current === 0) {
      frameRef.current = requestAnimationFrame(flushPendingWidth)
    }
  }
  const handlePointerUp = () => {
    flushPendingWidth()
    dragRef.current = null
    document.body.style.cursor = ''
  }
  const handleDoubleClick = () => onResize(defaultWidth)
  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault()
      onResize(width + direction * (event.key === 'ArrowRight' ? KEYBOARD_STEP : -KEYBOARD_STEP))
    }
  }

  return (
    <div
      role="separator"
      aria-orientation="vertical"
      aria-label={label}
      data-tip={`${label} : glisser ou flèches, double-clic pour la largeur par défaut`}
      aria-valuemin={min}
      aria-valuemax={max}
      aria-valuenow={width}
      tabIndex={0}
      className="group flex w-2 shrink-0 cursor-ew-resize justify-center focus-visible:outline-none"
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onPointerCancel={handlePointerUp}
      onDoubleClick={handleDoubleClick}
      onKeyDown={handleKeyDown}
    >
      <div className="h-full w-px bg-tily-line group-hover:w-0.5 group-hover:bg-tily-green group-focus-visible:w-0.5 group-focus-visible:bg-tily-focus" />
    </div>
  )
}
