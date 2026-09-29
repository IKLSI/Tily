import type { KeyboardEvent } from 'react'

const TAB_KEY = 'Tab'
const TABBABLE_SELECTOR = 'a[href], button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]'

const tabbablesIn = (container: HTMLElement): HTMLElement[] =>
  Array.from(container.querySelectorAll<HTMLElement>(TABBABLE_SELECTOR)).filter((element) => element.tabIndex >= 0 && element.getClientRects().length > 0)

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
