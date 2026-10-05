import { AgentState } from '../../../bridge/messages'
import type { CommandNotice } from '../commandStore'
import { commandNoticeTip } from '../commandNotices'
import { AgentStateIcon } from '../../agents/components/AgentStateIcon'

interface CommandNoticeIconProps {
  notice: CommandNotice
}

export function CommandNoticeIcon({ notice }: CommandNoticeIconProps) {
  return <AgentStateIcon state={notice.success ? AgentState.Done : AgentState.Error} tip={commandNoticeTip(notice)} />
}
