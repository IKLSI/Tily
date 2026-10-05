import { useRef } from 'react'
import { workspaceStateCounts } from '../../agents/agentSummary'
import { activeTab, type Workspace } from '../../../model/session'
import { useAgentStore } from '../../agents/agentStore'
import { HeaderWorkspaceItem } from './HeaderWorkspaceItem'
import { useStripWidths } from '../../../components/useStripWidths'
import { WorkspaceOverflow } from './WorkspaceOverflow'
import { visibleIndexes, type HeaderWorkspaceActions } from './workspaceStrip'

interface HeaderWorkspacesProps {
  workspaces: Workspace[]
  activeId: string
  renamingId: string | null
  actions: HeaderWorkspaceActions
}

const ID_SEPARATOR = '\n'

export function HeaderWorkspaces({ workspaces, activeId, renamingId, actions }: HeaderWorkspacesProps) {
  const agents = useAgentStore((state) => state.agents)
  const stripRef = useRef<HTMLElement>(null)
  const { space, overflow, items } = useStripWidths(stripRef, workspaces.map((workspace) => workspace.id).join(ID_SEPARATOR))
  const activeIndex = Math.max(0, workspaces.findIndex((workspace) => workspace.id === activeId))
  const renamingIndex = workspaces.findIndex((workspace) => workspace.id === renamingId)
  const pinnedIndex = renamingIndex >= 0 ? renamingIndex : activeIndex
  const shown = new Set(visibleIndexes(workspaces.map((workspace) => items[workspace.id] ?? 0), pinnedIndex, space, overflow))
  const hidden = workspaces.filter((_, index) => !shown.has(index))
  const names = workspaces.map((workspace) => workspace.name)
  const currentPaneId = workspaces[activeIndex] ? activeTab(workspaces[activeIndex]).active : undefined

  return (
    <nav ref={stripRef} aria-label="Workspaces" className="relative flex h-full min-w-0 flex-1 items-center gap-[4px]">
      <div className="relative flex h-full min-w-0 items-center gap-[4px] overflow-hidden">
        {workspaces.map((workspace, index) => (
          <HeaderWorkspaceItem
            key={workspace.id}
            workspace={workspace}
            siblings={names}
            counts={workspaceStateCounts(workspace, agents)}
            agents={agents}
            active={index === activeIndex}
            shown={shown.has(index)}
            renaming={workspace.id === renamingId}
            currentPaneId={index === activeIndex ? currentPaneId : undefined}
            actions={actions}
          />
        ))}
      </div>
      <WorkspaceOverflow workspaces={hidden} agents={agents} onSelect={actions.select} />
    </nav>
  )
}
