import { useCallback, useLayoutEffect, useState, type RefObject } from 'react'

const OVERSCAN_ROWS = 12

export interface VirtualRange {
  start: number
  end: number
}

export const useVirtualRows = (containerRef: RefObject<HTMLElement | null>, count: number, rowHeight: number): { range: VirtualRange; handleScroll: () => void } => {
  const [viewport, setViewport] = useState({ top: 0, height: 0 })

  const measure = useCallback(() => {
    const container = containerRef.current
    if (container) {
      setViewport((current) => (current.top === container.scrollTop && current.height === container.clientHeight ? current : { top: container.scrollTop, height: container.clientHeight }))
    }
  }, [containerRef])

  useLayoutEffect(() => {
    const container = containerRef.current
    if (!container) {
      return
    }
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(container)
    return () => observer.disconnect()
  }, [containerRef, measure])

  const start = Math.max(0, Math.floor(viewport.top / rowHeight) - OVERSCAN_ROWS)
  const end = Math.min(count, Math.ceil((viewport.top + viewport.height) / rowHeight) + OVERSCAN_ROWS)
  return { range: { start, end }, handleScroll: measure }
}
