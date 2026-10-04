import { OWNER_LABEL, ownerTip } from '../agents/agentOwnership'
import { useSessionStore } from '../store/sessionStore'
import { Icon } from './Icon'
import { IconName } from './iconName'

interface AgentOwnerMarkProps {
  owner: string
  compact?: boolean
}

const ICON_SIZE = 10

export function AgentOwnerMark({ owner, compact = false }: AgentOwnerMarkProps) {
  const tip = useSessionStore((state) => ownerTip(state.session, owner))
  if (compact) {
    return (
      <span className="shrink-0 text-tily-muted" data-tip={tip} aria-label={OWNER_LABEL}>
        <Icon name={IconName.Agent} size={ICON_SIZE} />
      </span>
    )
  }
  return (
    <span className="mr-1 inline-flex shrink-0 items-center gap-1 rounded border border-tily-line px-1 py-px text-[10px] leading-tight text-tily-muted @max-[420px]:hidden" data-tip={tip}>
      <Icon name={IconName.Agent} size={ICON_SIZE} />
      {OWNER_LABEL}
    </span>
  )
}
