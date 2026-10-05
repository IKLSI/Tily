import { useLayoutEffect, useRef } from 'react'
import { ActionMenu, type ActionMenuItem } from './ActionMenu'

interface FloatingMenuProps {
  x: number
  y: number
  label: string
  items: ActionMenuItem[]
  onClose: () => void
}

const EDGE_MARGIN_PX = 8

export function FloatingMenu({ x, y, label, items, onClose }: FloatingMenuProps) {
  const anchorRef = useRef<HTMLDivElement>(null)

  useLayoutEffect(() => {
    const anchor = anchorRef.current
    const menu = anchor?.querySelector<HTMLElement>('[role="menu"]')
    if (!anchor || !menu) {
      return
    }
    const { width, height } = menu.getBoundingClientRect()
    anchor.style.left = `${Math.max(EDGE_MARGIN_PX, Math.min(x, window.innerWidth - EDGE_MARGIN_PX - width))}px`
    anchor.style.top = `${y + height > window.innerHeight - EDGE_MARGIN_PX ? Math.max(EDGE_MARGIN_PX, y - height) : y}px`
  }, [x, y])

  return (
    <div ref={anchorRef} className="fixed z-30" style={{ left: x, top: y }}>
      <div className="relative">
        <ActionMenu label={label} items={items} onClose={onClose} />
      </div>
    </div>
  )
}
