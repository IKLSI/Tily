import type { KeyboardEvent } from 'react'

const TAB_KEY = 'Tab'
const TABBABLE_SELECTOR = 'a[href], button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]'

const ALL_ELEMENTS = '*'
const SCROLLING_OVERFLOWS = new Set(['auto', 'scroll'])

const scrollsByKeyboard = (element: HTMLElement): boolean => {
  const overflowsY = element.scrollHeight > element.clientHeight
  const overflowsX = element.scrollWidth > element.clientWidth
  if (!overflowsY && !overflowsX) {
    return false
  }
  const { overflowX, overflowY } = getComputedStyle(element)
  const scrolls = (overflowsY && SCROLLING_OVERFLOWS.has(overflowY)) || (overflowsX && SCROLLING_OVERFLOWS.has(overflowX))
  return scrolls && element.querySelector(TABBABLE_SELECTOR) === null
}

const tabbablesIn = (container: HTMLElement): HTMLElement[] =>
  Array.from(container.querySelectorAll<HTMLElement>(ALL_ELEMENTS)).filter(
    (element) => element.getClientRects().length > 0 && ((element.matches(TABBABLE_SELECTOR) && element.tabIndex >= 0) || scrollsByKeyboard(element)),
  )

export const keepTabInside = (event: KeyboardEvent<HTMLElement>): void => {
  if (event.key !== TAB_KEY || event.ctrlKey || event.altKey || event.metaKey) {
    return
  }
  const tabbables = tabbablesIn(event.currentTarget)
  const first = tabbables[0]
  const last = tabbables[tabbables.length - 1]
  if (!first || !last) {
    event.preventDefault()
  } else if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first.focus()
  }
}
