export interface HeaderWorkspaceActions {
  select: (workspaceId: string) => void
  joinPane: (paneId: string) => void
  startRename: (workspaceId: string) => void
  commitRename: (name: string) => void
  cancelRename: () => void
}

const STRIP_GAP_PX = 4

const spanOf = (widths: number[]): number => widths.reduce((total, width) => total + width, 0) + STRIP_GAP_PX * Math.max(widths.length - 1, 0)

const leadingIndexes = (count: number, pinnedIndex: number): number[] => {
  const leading = Array.from({ length: count }, (_, index) => index)
  return pinnedIndex < count ? leading : [...leading.slice(0, count - 1), pinnedIndex]
}

export const visibleIndexes = (widths: number[], pinnedIndex: number, space: number, overflowWidth: number): number[] => {
  if (spanOf(widths) <= space) {
    return widths.map((_, index) => index)
  }
  for (let count = widths.length - 1; count > 1; count -= 1) {
    const indexes = leadingIndexes(count, pinnedIndex)
    if (spanOf(indexes.map((index) => widths[index])) + STRIP_GAP_PX + overflowWidth <= space) {
      return indexes
    }
  }
  return [pinnedIndex]
}
