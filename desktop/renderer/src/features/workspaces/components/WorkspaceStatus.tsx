import type { MouseEvent } from 'react'
import { AgentState } from '../../../bridge/messages'
import { STATE_LABELS, stateBreakdown, workspaceSummary, type StateCounts } from '../../agents/agentSummary'
import { AgentStateIcon } from '../../agents/components/AgentStateIcon'

interface WorkspaceStatusProps {
  counts: StateCounts
  onJoin?: (state: AgentState) => void
}

const PILL = 'flex h-4 items-center gap-[3px] rounded-full pr-1.5 pl-[3px] text-[11px] leading-none font-semibold tabular-nums'

const PILL_CLASSES: Partial<Record<AgentState, string>> = {
  [AgentState.Waiting]: 'bg-tily-status-waiting/15 text-tily-status-waiting',
  [AgentState.Error]: 'bg-tily-status-error/15 text-tily-status-error',
}

export function WorkspaceStatus({ counts, onJoin }: WorkspaceStatusProps) {
  const { alerts, activity } = workspaceSummary(counts)
  if (alerts.length === 0 && !activity) {
    return null
  }
  const breakdown = stateBreakdown(counts)
  return (
    <span className="flex shrink-0 items-center gap-1.5" data-tip={breakdown}>
      {alerts.map(({ state, count }) => {
        const content = (
          <>
            <span className="pointer-events-none flex">
              <AgentStateIcon state={state} size={10} />
            </span>
            {count}
          </>
        )
        if (!onJoin) {
          return (
            <span key={state} className={`${PILL} ${PILL_CLASSES[state]}`}>
              {content}
            </span>
          )
        }
        const label = `Rejoindre le terminal ${STATE_LABELS[state].toLowerCase()}`
        const handleJoin = (event: MouseEvent) => {
          event.stopPropagation()
          onJoin(state)
        }
        return (
          <button
            key={state}
            type="button"
            className={`${PILL} cursor-pointer hover:brightness-125 ${PILL_CLASSES[state]}`}
            aria-label={`${label} (${count})`}
            data-tip={label}
            onClick={handleJoin}
          >
            {content}
          </button>
        )
      })}
      {activity && <AgentStateIcon state={activity} tip={breakdown} />}
    </span>
  )
}
