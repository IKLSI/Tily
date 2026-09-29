import type { GitCommit, GitRefLabel } from '../bridge/gitMessages'
import { GIT_COLUMN_MIN, type GitGraphLayout } from '../model/session'

export enum GitNodeKind {
  Commit = 'commit',
  Merge = 'merge',
  Stash = 'stash',
  WorkingTree = 'workingTree',
}

export enum ResizerSide {
  Left = 'left',
  Right = 'right',
}

export const GRAPH_ROW_HEIGHT = 28
export const GRAPH_HEADER_HEIGHT = 26
export const GRAPH_LANE_WIDTH = 20
const GRAPH_LANE_PADDING = 8
export const WORKING_TREE_KEY = 'wip'
const MESSAGE_MIN_WIDTH = 200
const LABELS_FIT_MIN_WIDTH = 88
export const ROW_FOCUS_OUTLINE = 'group-focus-visible:outline-2 group-focus-visible:-outline-offset-2 group-focus-visible:outline-dock-focus'

const LANE_STROKES = ['stroke-dock-lane-0', 'stroke-dock-lane-1', 'stroke-dock-lane-2', 'stroke-dock-lane-3', 'stroke-dock-lane-4', 'stroke-dock-lane-5', 'stroke-dock-lane-6', 'stroke-dock-lane-7']
const LANE_FILLS = ['fill-dock-lane-0', 'fill-dock-lane-1', 'fill-dock-lane-2', 'fill-dock-lane-3', 'fill-dock-lane-4', 'fill-dock-lane-5', 'fill-dock-lane-6', 'fill-dock-lane-7']
const LANE_BARS = ['bg-dock-lane-0', 'bg-dock-lane-1', 'bg-dock-lane-2', 'bg-dock-lane-3', 'bg-dock-lane-4', 'bg-dock-lane-5', 'bg-dock-lane-6', 'bg-dock-lane-7']
const LANE_TINTS = ['bg-dock-lane-0/25', 'bg-dock-lane-1/25', 'bg-dock-lane-2/25', 'bg-dock-lane-3/25', 'bg-dock-lane-4/25', 'bg-dock-lane-5/25', 'bg-dock-lane-6/25', 'bg-dock-lane-7/25']
const LANE_STRONG_TINTS = ['bg-dock-lane-0/45', 'bg-dock-lane-1/45', 'bg-dock-lane-2/45', 'bg-dock-lane-3/45', 'bg-dock-lane-4/45', 'bg-dock-lane-5/45', 'bg-dock-lane-6/45', 'bg-dock-lane-7/45']

const LANE_HOVER_TINTS = ['hover:bg-dock-lane-0/45', 'hover:bg-dock-lane-1/45', 'hover:bg-dock-lane-2/45', 'hover:bg-dock-lane-3/45', 'hover:bg-dock-lane-4/45', 'hover:bg-dock-lane-5/45', 'hover:bg-dock-lane-6/45', 'hover:bg-dock-lane-7/45']
const LANE_STRONG_HOVER_TINTS = ['hover:bg-dock-lane-0/65', 'hover:bg-dock-lane-1/65', 'hover:bg-dock-lane-2/65', 'hover:bg-dock-lane-3/65', 'hover:bg-dock-lane-4/65', 'hover:bg-dock-lane-5/65', 'hover:bg-dock-lane-6/65', 'hover:bg-dock-lane-7/65']

const pick = (classes: string[], color: number): string => classes[color % classes.length]

const messageWidth = (layout: GitGraphLayout, width: number, authorShown: boolean, dateShown: boolean): number =>
  width - layout.labelsWidth - layout.graphWidth - (authorShown ? layout.authorWidth : 0) - (dateShown ? layout.dateWidth : 0)

const narrowed = (current: number, floor: number, deficit: number): number => (deficit > 0 ? Math.max(Math.min(current, floor), current - deficit) : current)

export const fitGraphColumns = (layout: GitGraphLayout, width: number, lanes: number): Pick<GitGraphLayout, 'authorShown' | 'dateShown' | 'labelsWidth' | 'graphWidth'> => {
  const fits = (authorShown: boolean, dateShown: boolean): boolean => width <= 0 || messageWidth(layout, width, authorShown, dateShown) >= MESSAGE_MIN_WIDTH
  const dateShown = layout.dateShown && fits(layout.authorShown, true)
  const authorShown = layout.authorShown && fits(true, dateShown)
  const deficit = width > 0 ? MESSAGE_MIN_WIDTH - messageWidth(layout, width, authorShown, dateShown) : 0
  const labelsWidth = narrowed(layout.labelsWidth, LABELS_FIT_MIN_WIDTH, deficit)
  const graphWidth = narrowed(layout.graphWidth, Math.max(GIT_COLUMN_MIN, lanes * GRAPH_LANE_WIDTH + GRAPH_LANE_PADDING), deficit - (layout.labelsWidth - labelsWidth))
  return { authorShown, dateShown, labelsWidth, graphWidth }
}

export const laneStroke = (color: number): string => pick(LANE_STROKES, color)

export const laneFill = (color: number): string => pick(LANE_FILLS, color)

export const laneBar = (color: number): string => pick(LANE_BARS, color)

export const laneTint = (color: number, strong: boolean): string => pick(strong ? LANE_STRONG_TINTS : LANE_TINTS, color)

export const laneHoverTint = (color: number, strong: boolean): string => pick(strong ? LANE_STRONG_HOVER_TINTS : LANE_HOVER_TINTS, color)

export const graphRowId = (key: string): string => `git-graph-row-${key}`

export interface GitGraphRowHandlers {
  select: (commit: GitCommit) => void
  openMenu: (commit: GitCommit, x: number, y: number) => void
  openLabelMenu: (label: GitRefLabel, x: number, y: number) => void
  activateLabel: (label: GitRefLabel) => void
}
