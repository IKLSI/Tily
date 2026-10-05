import { AgentState, type PaneAgent } from '../../../bridge/messages'
import { agentLabel, describeAgent, STATE_LABELS } from '../agentSummary'
import { AgentStateIcon } from './AgentStateIcon'

interface AgentBadgeProps {
  agent: PaneAgent
}

const STATE_CLASSES: Record<AgentState, string> = {
  [AgentState.Working]: 'border-tily-status-working text-tily-status-working',
  [AgentState.Waiting]: 'border-tily-status-waiting text-tily-status-waiting',
  [AgentState.Done]: 'border-tily-status-done text-tily-status-done',
  [AgentState.Error]: 'border-tily-status-error text-tily-status-error',
  [AgentState.Unknown]: 'border-tily-status-unknown text-tily-status-unknown',
}

export function AgentBadge({ agent }: AgentBadgeProps) {
  return (
    <span role="status" className={`mr-1 inline-flex min-w-0 items-center gap-1 rounded border px-1.5 py-px text-[10px] leading-tight font-medium ${STATE_CLASSES[agent.state]}`} data-tip={describeAgent(agent)}>
      <AgentStateIcon state={agent.state} tip={describeAgent(agent)} size={11} />
      <span className="truncate @max-[420px]:hidden">{`${agentLabel(agent)} · ${STATE_LABELS[agent.state]}`}</span>
    </span>
  )
}
