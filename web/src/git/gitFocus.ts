const PANEL_SELECTOR = '[data-git-panel]'
const GRAPH_LIST_SELECTOR = '[data-git-graph-list]'
const ROW_SELECTOR = '[data-git-row]'
const BUTTON_SELECTOR = 'button:not([aria-disabled="true"])'

let graphFocusPending = false

export const gitPanel = (): HTMLElement | null => document.querySelector<HTMLElement>(PANEL_SELECTOR)

export const isInGitPanel = (element: Element | null): boolean => Boolean(element?.closest(PANEL_SELECTOR))

export const focusGitPanel = (): void => {
  const panel = gitPanel()
  const target = panel?.querySelector<HTMLElement>(`${ROW_SELECTOR}[tabindex="0"]`) ?? panel?.querySelector<HTMLElement>(BUTTON_SELECTOR) ?? panel
  target?.focus()
}

export const focusGitGraph = (): boolean => {
  const list = document.querySelector<HTMLElement>(GRAPH_LIST_SELECTOR)
  list?.focus()
  return Boolean(list)
}

export const requestGraphFocus = (): void => {
  graphFocusPending = !focusGitGraph()
  if (graphFocusPending) {
    focusGitPanel()
  }
}

export const takeGraphFocusRequest = (): boolean => {
  const pending = graphFocusPending
  graphFocusPending = false
  return pending
}

export const focusGitRow = (key: string): boolean => {
  const row = Array.from(gitPanel()?.querySelectorAll<HTMLElement>(ROW_SELECTOR) ?? []).find((candidate) => candidate.dataset.gitRow === key)
  row?.focus()
  return Boolean(row)
}

export const refocusGitIfLost = (): void => {
  requestAnimationFrame(() => {
    if (document.activeElement === document.body) {
      focusGitPanel()
    }
  })
}
