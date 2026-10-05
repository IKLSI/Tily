export enum Direction {
  Left = 'left',
  Right = 'right',
  Up = 'up',
  Down = 'down',
}

const PANE_SELECTOR = '[data-pane-id]'
const NO_OVERLAP_PENALTY = 2

interface PaneRect {
  id: string
  rect: DOMRect
}

const paneRects = (): PaneRect[] =>
  Array.from(document.querySelectorAll<HTMLElement>(PANE_SELECTOR)).map((element) => ({ id: element.dataset.paneId ?? '', rect: element.getBoundingClientRect() }))

const forwardGap = (from: DOMRect, to: DOMRect, direction: Direction): number => {
  switch (direction) {
    case Direction.Left:
      return from.left - to.right
    case Direction.Right:
      return to.left - from.right
    case Direction.Up:
      return from.top - to.bottom
    case Direction.Down:
      return to.top - from.bottom
  }
}

const sideGap = (from: DOMRect, to: DOMRect, direction: Direction): number =>
  direction === Direction.Left || direction === Direction.Right
    ? Math.max(0, Math.max(from.top, to.top) - Math.min(from.bottom, to.bottom))
    : Math.max(0, Math.max(from.left, to.left) - Math.min(from.right, to.right))

export const paneInDirection = (activePaneId: string, direction: Direction): string | undefined => {
  const rects = paneRects()
  const origin = rects.find((pane) => pane.id === activePaneId)?.rect
  if (!origin) {
    return undefined
  }
  let best: { id: string; score: number } | undefined
  for (const { id, rect } of rects) {
    const gap = forwardGap(origin, rect, direction)
    if (id === activePaneId || gap < 0) {
      continue
    }
    const score = gap + sideGap(origin, rect, direction) * NO_OVERLAP_PENALTY
    if (!best || score < best.score) {
      best = { id, score }
    }
  }
  return best?.id
}
