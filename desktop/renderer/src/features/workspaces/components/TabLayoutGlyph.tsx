import { isLeaf, SplitAxis, type SplitNode } from '../../../model/session'

interface TabLayoutGlyphProps {
  tree: SplitNode
}

const WIDTH = 14
const HEIGHT = 11
const INSET = 1
const HALF_PIXEL = 0.5

const dividersOf = (node: SplitNode, left: number, top: number, width: number, height: number): string[] => {
  if (isLeaf(node)) {
    return []
  }
  if (node.axis === SplitAxis.Horizontal) {
    const middle = Math.floor(left + width * node.ratio) + HALF_PIXEL
    return [`M${middle} ${top}V${top + height}`, ...dividersOf(node.a, left, top, middle - left, height), ...dividersOf(node.b, middle, top, left + width - middle, height)]
  }
  const middle = Math.floor(top + height * node.ratio) + HALF_PIXEL
  return [`M${left} ${middle}H${left + width}`, ...dividersOf(node.a, left, top, width, middle - top), ...dividersOf(node.b, left, middle, width, top + height - middle)]
}

export function TabLayoutGlyph({ tree }: TabLayoutGlyphProps) {
  if (isLeaf(tree)) {
    return null
  }
  return (
    <svg width={WIDTH} height={HEIGHT} viewBox={`0 0 ${WIDTH} ${HEIGHT}`} fill="none" stroke="currentColor" strokeWidth={1} aria-hidden="true">
      <rect x={HALF_PIXEL} y={HALF_PIXEL} width={WIDTH - 1} height={HEIGHT - 1} rx={2} />
      <path d={dividersOf(tree, INSET, INSET, WIDTH - 2 * INSET, HEIGHT - 2 * INSET).join('')} />
    </svg>
  )
}
