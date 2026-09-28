import { memo, type MouseEvent } from 'react'
import type { GitCommit } from '../bridge/gitMessages'
import { fullDate, initials, relativeDate, shortDate, shortSha } from '../git/gitLabels'
import type { GitGraphLayout } from '../model/session'
import { GitGraphCell } from './GitGraphCell'
import { GitNodeKind, GRAPH_ROW_HEIGHT, graphRowId, laneBar, ROW_FOCUS_OUTLINE, type GitGraphRowHandlers } from './gitGraphStyles'
import { GitRefLabels } from './GitRefLabels'
import { Icon } from './Icon'
import { IconName } from './iconName'

interface GitGraphRowProps {
  commit: GitCommit
  index: number
  selected: boolean
  head: boolean
  layout: GitGraphLayout
  remotes: string[]
  handlers: GitGraphRowHandlers
}

const nodeKind = (commit: GitCommit): GitNodeKind => {
  if (commit.stash) {
    return GitNodeKind.Stash
  }
  return commit.parents.length > 1 ? GitNodeKind.Merge : GitNodeKind.Commit
}

const subjectTone = (commit: GitCommit, head: boolean): string => {
  if (head) {
    return 'font-semibold text-dock-ink'
  }
  return commit.stash || commit.parents.length > 1 ? 'text-dock-muted' : 'text-dock-ink-soft'
}

export const GitGraphRow = memo(function GitGraphRow({ commit, index, selected, head, layout, remotes, handlers }: GitGraphRowProps) {
  const handleClick = () => handlers.select(commit)
  const handleContextMenu = (event: MouseEvent) => {
    event.preventDefault()
    handlers.openMenu(commit, event.clientX, event.clientY)
  }

  return (
    <div
      id={graphRowId(commit.sha)}
      role="option"
      aria-selected={selected}
      className={`absolute inset-x-0 flex items-center text-[12px] select-none ${selected ? `bg-dock-green-soft ${ROW_FOCUS_OUTLINE}` : 'hover:bg-dock-green-hover'}`}
      style={{ top: index * GRAPH_ROW_HEIGHT, height: GRAPH_ROW_HEIGHT }}
      onClick={handleClick}
      onContextMenu={handleContextMenu}
    >
      <GitRefLabels refs={commit.refs} color={commit.graph.color} width={layout.labelsWidth} remotes={remotes} handlers={handlers} />
      <GitGraphCell graph={commit.graph} width={layout.graphWidth} node={nodeKind(commit)} initials={initials(commit.author)} labelled={commit.refs.length > 0} />
      <div className="flex min-w-0 flex-1 items-center gap-[8px] self-stretch pr-[8px]">
        <span className={`w-[3px] shrink-0 self-stretch opacity-60 ${laneBar(commit.graph.color)}`} />
        {commit.stash && <Icon name={IconName.Stash} className="shrink-0 text-dock-muted" />}
        <span className={`min-w-0 truncate ${subjectTone(commit, head)}`} data-tip={`${shortSha(commit.sha)} · ${commit.subject}`}>
          {commit.subject}
        </span>
      </div>
      {layout.authorShown && (
        <span className="shrink-0 truncate px-[8px] text-dock-muted" style={{ width: layout.authorWidth }} data-tip={commit.email}>
          {commit.author}
        </span>
      )}
      {layout.dateShown && (
        <span className="shrink-0 truncate px-[8px] text-dock-muted tabular-nums" style={{ width: layout.dateWidth }} data-tip={`${fullDate(commit.date)} (${relativeDate(commit.date)})`}>
          {shortDate(commit.date)}
        </span>
      )}
    </div>
  )
})
