import { useLayoutEffect, useState, type RefObject } from 'react'

interface StripWidths {
  space: number
  overflow: number
  items: Record<string, number>
}

const ITEM_SELECTOR = '[data-strip-item]'
const OVERFLOW_SELECTOR = '[data-strip-overflow]'
const EMPTY_WIDTHS: StripWidths = { space: 0, overflow: 0, items: {} }

const widthOf = (element: Element | null): number => element?.getBoundingClientRect().width ?? 0

const measure = (strip: HTMLElement): StripWidths => ({
  space: widthOf(strip),
  overflow: widthOf(strip.querySelector(OVERFLOW_SELECTOR)),
  items: Object.fromEntries(Array.from(strip.querySelectorAll<HTMLElement>(ITEM_SELECTOR), (item) => [item.dataset.stripItem ?? '', widthOf(item)])),
})

const sameWidths = (current: StripWidths, next: StripWidths): boolean => {
  const ids = Object.keys(next.items)
  return current.space === next.space && current.overflow === next.overflow && ids.length === Object.keys(current.items).length && ids.every((id) => current.items[id] === next.items[id])
}

export function useStripWidths(stripRef: RefObject<HTMLElement | null>, itemsKey: string): StripWidths {
  const [widths, setWidths] = useState(EMPTY_WIDTHS)

  useLayoutEffect(() => {
    const strip = stripRef.current
    if (!strip) {
      return
    }
    const update = () => {
      if (widthOf(strip) === 0) {
        return
      }
      const next = measure(strip)
      setWidths((current) => (sameWidths(current, next) ? current : next))
    }
    update()
    const observer = new ResizeObserver(update)
    observer.observe(strip)
    strip.querySelectorAll(`${ITEM_SELECTOR}, ${OVERFLOW_SELECTOR}`).forEach((element) => observer.observe(element))
    return () => observer.disconnect()
  }, [stripRef, itemsKey])

  return widths
}
