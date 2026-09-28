import { GitSegmentKind, type GitGraphRow, type GitGraphSegment } from '../bridge/gitMessages'
import { GitNodeKind, GRAPH_ROW_HEIGHT, laneFill, laneStroke } from './gitGraphStyles'

interface GitGraphCellProps {
  graph: GitGraphRow
  width: number
  node: GitNodeKind
  initials?: string
  labelled: boolean
}

const LANE_WIDTH = 20
const NODE_RADIUS = 9
const MERGE_RADIUS = 4
const STASH_SIZE = 14
const DASH = '3 3'
const MIDDLE = GRAPH_ROW_HEIGHT / 2

const laneX = (lane: number): number => lane * LANE_WIDTH + LANE_WIDTH / 2

const segmentPath = ({ from, to, kind }: GitGraphSegment): string => {
  const [start, end] = [laneX(from), laneX(to)]
  if (kind === GitSegmentKind.Through) {
    return `M${start} 0V${GRAPH_ROW_HEIGHT}`
  }
  if (kind === GitSegmentKind.In) {
    return start === end ? `M${start} 0V${MIDDLE}` : `M${start} 0C${start} ${MIDDLE} ${end} 0 ${end} ${MIDDLE}`
  }
  return start === end ? `M${start} ${MIDDLE}V${GRAPH_ROW_HEIGHT}` : `M${start} ${MIDDLE}C${start} ${GRAPH_ROW_HEIGHT} ${end} ${MIDDLE} ${end} ${GRAPH_ROW_HEIGHT}`
}

export function GitGraphCell({ graph, width, node, initials, labelled }: GitGraphCellProps) {
  const x = laneX(graph.lane)
  const stroke = laneStroke(graph.color)

  const renderNode = () => {
    switch (node) {
      case GitNodeKind.Merge:
        return <circle cx={x} cy={MIDDLE} r={MERGE_RADIUS} className={laneFill(graph.color)} />
      case GitNodeKind.Stash:
        return <rect x={x - STASH_SIZE / 2} y={MIDDLE - STASH_SIZE / 2} width={STASH_SIZE} height={STASH_SIZE} rx={3} strokeWidth={1.5} strokeDasharray={DASH} className={`fill-dock-panel ${stroke}`} />
      case GitNodeKind.WorkingTree:
        return <circle cx={x} cy={MIDDLE} r={NODE_RADIUS} strokeWidth={1.5} strokeDasharray={DASH} className={`fill-dock-panel ${stroke}`} />
      default:
        return (
          <>
            <circle cx={x} cy={MIDDLE} r={NODE_RADIUS} className={laneFill(graph.color)} />
            <text x={x} y={MIDDLE} dy="0.35em" textAnchor="middle" fontSize={8.5} fontWeight={700} className="fill-dock-paper">
              {initials}
            </text>
          </>
        )
    }
  }

  return (
    <svg width={width} height={GRAPH_ROW_HEIGHT} className="shrink-0 overflow-hidden" aria-hidden="true">
      {labelled && <path d={`M0 ${MIDDLE}H${x}`} strokeWidth={1.5} className={stroke} />}
      {graph.segments.map((segment, index) => (
        <path key={index} d={segmentPath(segment)} fill="none" strokeWidth={2} strokeDasharray={segment.dashed ? DASH : undefined} className={laneStroke(segment.color)} />
      ))}
      {renderNode()}
    </svg>
  )
}
