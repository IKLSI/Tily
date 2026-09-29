import type { KeyboardEvent, MouseEvent, PointerEvent } from 'react'
import { nextPaneInState, tabAgents, type AgentMap } from '../agents/agentSummary'
import { activePane, DEFAULT_SHELL, panesOf, type Tab } from '../model/session'
import { AgentStateIcon } from './AgentStateIcon'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { InlineNameEditor } from './InlineNameEditor'
import { TabLayoutGlyph } from './TabLayoutGlyph'
import { beginTabDrag } from './tabDrag'
import { TruncatedName } from './TruncatedName'
import { COLLAPSE_KEY, focusOwnWorkspaceRow, isMenuKey, menuRequestFor, MOVE_KEYS, PANEL_CLOSE_BUTTON, PANEL_DROP_LINE, type PanelMenuRequest, type WorkspacePanelActions } from './workspacePanel'

interface WorkspaceTabRowProps {
  workspaceId: string
  tab: Tab
  siblings: string[]
  active: boolean
  renaming: boolean
  dragging: boolean
  dropBefore: boolean
  currentPaneId: string | undefined
  agents: AgentMap
  actions: WorkspacePanelActions
  onOpenMenu: (request: PanelMenuRequest) => void
}

const MIDDLE_BUTTON = 1
const JOIN_TARGET = '[data-join]'
const LEAD = 'flex w-[14px] shrink-0 justify-center text-dock-muted'
const SHELL_LABELS: Record<string, string> = { pwsh: 'pwsh', cmd: 'cmd', gitbash: 'bash' }
const SHELL_NAMES: Record<string, string> = { pwsh: 'PowerShell 7', cmd: 'Invite de commandes', gitbash: 'Git Bash' }

const paneCountLabel = (count: number): string => (count === 1 ? '1 pane' : `${count} panes`)

export function WorkspaceTabRow({ workspaceId, tab, siblings, active, renaming, dragging, dropBefore, currentPaneId, agents, actions, onOpenMenu }: WorkspaceTabRowProps) {
  const summary = tabAgents(tab, agents)
  const shell = activePane(tab).shell
  const customShell = shell === DEFAULT_SHELL ? null : shell
  const tip = [tab.name, customShell && (SHELL_NAMES[customShell] ?? customShell), paneCountLabel(panesOf(tab.tree).length), summary?.tip].filter(Boolean).join(' · ')
  const rowState = active ? 'bg-dock-green-soft text-dock-green-deep' : 'text-dock-ink-soft hover:bg-dock-panel hover:text-dock-ink'
  const lead = summary ? <AgentStateIcon state={summary.state} tip={summary.tip} /> : <TabLayoutGlyph tree={tab.tree} />

  const joinTargetOf = (event: MouseEvent): string | undefined =>
    summary && event.target instanceof Element && event.target.closest(JOIN_TARGET) ? nextPaneInState([tab], agents, summary.state, currentPaneId) : undefined
  const handleClick = (event: MouseEvent) => {
    const paneId = joinTargetOf(event)
    if (paneId) {
      actions.joinPane(paneId)
    } else {
      actions.selectTab(workspaceId, tab.id)
    }
  }
  const handleRename = () => actions.startRenameTab(tab.id)
  const handleClose = () => actions.closeTab(tab.id)
  const handleAuxClick = (event: MouseEvent) => {
    if (event.button === MIDDLE_BUTTON) {
      event.preventDefault()
      actions.closeTab(tab.id)
    }
  }
  const handlePointerDown = (event: PointerEvent<HTMLElement>) => beginTabDrag(event, tab.id, actions.moveTab)
  const handleKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (event.key === 'F2') {
      event.preventDefault()
      handleRename()
    } else if (isMenuKey(event)) {
      event.preventDefault()
      onOpenMenu(menuRequestFor(event, workspaceId, tab.id))
    } else if (event.altKey && event.key in MOVE_KEYS) {
      event.preventDefault()
      actions.shiftTab(tab.id, MOVE_KEYS[event.key])
    } else if (!event.altKey && event.key === COLLAPSE_KEY) {
      event.preventDefault()
      focusOwnWorkspaceRow(event.currentTarget)
    }
  }
  const handleContextMenu = (event: MouseEvent) => {
    event.preventDefault()
    onOpenMenu({ workspaceId, tabId: tab.id, x: event.clientX, y: event.clientY, returnFocus: null })
  }
  const stopPropagation = (event: MouseEvent) => event.stopPropagation()

  return (
    <li className="relative">
      {dropBefore && <span aria-hidden="true" className={PANEL_DROP_LINE} />}
      <div
        data-drop-workspace={workspaceId}
        data-drop-tab={tab.id}
        className={`group relative flex h-[28px] items-center rounded-md pr-[2px] pl-[6px] select-none before:absolute before:top-1/2 before:-left-[7px] before:h-px before:w-[6px] before:bg-dock-line ${rowState} ${dragging ? 'opacity-40' : ''}`}
        onContextMenu={handleContextMenu}
      >
        {renaming ? (
          <span className="flex min-w-0 flex-1 items-center gap-[8px]" onContextMenu={stopPropagation}>
            <span className={LEAD}>{lead}</span>
            <InlineNameEditor value={tab.name} label="Nom de l’onglet" className="h-[22px] min-w-0 flex-1 text-[13px]" onCommit={actions.commitRenameTab} onCancel={actions.cancelRenameTab} />
          </span>
        ) : (
          <button
            type="button"
            aria-current={active || undefined}
            data-panel-row=""
            data-row-name={tab.name}
            data-tip={tip}
            className="flex h-full min-w-0 flex-1 cursor-pointer items-center gap-[8px] text-left text-[13px]"
            onClick={handleClick}
            onDoubleClick={handleRename}
            onAuxClick={handleAuxClick}
            onPointerDown={handlePointerDown}
            onKeyDown={handleKeyDown}
          >
            <span className={LEAD} data-join={summary ? true : undefined}>
              {lead}
            </span>
            <TruncatedName name={tab.name} siblings={siblings} className="flex-1" />
            {customShell && <span className="shrink-0 font-mono text-[10.5px] text-dock-muted @max-[260px]:hidden">{SHELL_LABELS[customShell] ?? customShell}</span>}
          </button>
        )}
        <button type="button" className={PANEL_CLOSE_BUTTON} data-tip="Fermer l’onglet" aria-label={`Fermer l’onglet ${tab.name}`} onClick={handleClose}>
          <Icon name={IconName.Close} size={10} />
        </button>
      </div>
    </li>
  )
}
