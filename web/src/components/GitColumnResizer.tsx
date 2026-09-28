import { useRef, type KeyboardEvent, type MouseEvent, type PointerEvent } from 'react'
import { GIT_COLUMN_MAX, GIT_COLUMN_MIN } from '../model/session'
import { ResizerSide } from './gitGraphStyles'

interface GitColumnResizerProps {
  side: ResizerSide
  width: number
  label: string
  onResize: (width: number) => void
}

const KEYBOARD_STEP = 10

const stopClick = (event: MouseEvent) => event.stopPropagation()

export function GitColumnResizer({ side, width, label, onResize }: GitColumnResizerProps) {
  const dragRef = useRef<{ startX: number; startWidth: number } | null>(null)
  const direction = side === ResizerSide.Right ? 1 : -1

  const handlePointerDown = (event: PointerEvent<HTMLSpanElement>) => {
    event.preventDefault()
    event.stopPropagation()
    event.currentTarget.setPointerCapture(event.pointerId)
    dragRef.current = { startX: event.clientX, startWidth: width }
    document.body.style.cursor = 'ew-resize'
  }
  const handlePointerMove = (event: PointerEvent<HTMLSpanElement>) => {
    if (dragRef.current) {
      onResize(dragRef.current.startWidth + direction * (event.clientX - dragRef.current.startX))
    }
  }
  const handlePointerUp = () => {
    dragRef.current = null
    document.body.style.cursor = ''
  }
  const handleKeyDown = (event: KeyboardEvent<HTMLSpanElement>) => {
    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault()
      event.stopPropagation()
      onResize(width + direction * (event.key === 'ArrowRight' ? KEYBOARD_STEP : -KEYBOARD_STEP))
    }
  }

  return (
    <span
      role="separator"
      aria-orientation="vertical"
      aria-label={label}
      aria-valuemin={GIT_COLUMN_MIN}
      aria-valuemax={GIT_COLUMN_MAX}
      aria-valuenow={width}
      tabIndex={0}
      data-tip={`${label} (glisser ou flèches)`}
      className={`group absolute inset-y-0 z-10 flex w-[7px] cursor-ew-resize justify-center focus-visible:outline-none ${side === ResizerSide.Right ? '-right-[4px]' : '-left-[4px]'}`}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onPointerCancel={handlePointerUp}
      onClick={stopClick}
      onKeyDown={handleKeyDown}
    >
      <span className="h-full w-px bg-dock-line group-hover:w-0.5 group-hover:bg-dock-green group-focus-visible:w-0.5 group-focus-visible:bg-dock-focus" />
    </span>
  )
}
