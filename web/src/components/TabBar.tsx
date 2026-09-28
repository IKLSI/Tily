import { Fragment, useCallback, useEffect, useRef, useState, type KeyboardEvent, type MouseEvent, type PointerEvent, type WheelEvent } from 'react'
import { useShallow } from 'zustand/react/shallow'
import type { ShellProfile } from '../bridge/messages'
import { tabAgents } from '../agents/agentSummary'
import { DEFAULT_SHELL, type Workspace } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useUiStore } from '../store/uiStore'
import { AgentStateIcon } from './AgentStateIcon'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { InlineNameEditor } from './InlineNameEditor'
import { ShellMenu } from './ShellMenu'
import { TabContextMenu, type TabMenuActions, type TabMenuRequest } from './TabContextMenu'
import { beginTabDrag, isDropTarget, type MoveTabHandler } from './tabDrag'

interface TabBarProps {
  workspace: Workspace
  shells: ShellProfile[]
  renamingTabId: string | null
  panelOpen: boolean
  onTogglePanel: () => void
  onSelect: (tabId: string) => void
  onStartRename: (tabId: string) => void
  onCommitRename: (name: string) => void
  onCancelRename: () => void
  onClose: (tabId: string) => void
  onCloseOthers: (tabId: string) => void
  onShift: (tabId: string, offset: number) => void
  onDuplicate: (tabId: string) => void
  onNew: (shellId: string) => void
  onMove: MoveTabHandler
}

const MIDDLE_BUTTON = 1

const tabSelector = (tabId: string): string => `[data-drop-tab="${tabId}"]`

const isMenuKey = (event: KeyboardEvent): boolean => (event.shiftKey && event.key === 'F10') || event.key === 'ContextMenu'

export function TabBar({ workspace, shells, renamingTabId, panelOpen, onTogglePanel, onSelect, onStartRename, onCommitRename, onCancelRename, onClose, onCloseOthers, onShift, onDuplicate, onNew, onMove }: TabBarProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [tabMenu, setTabMenu] = useState<TabMenuRequest | null>(null)
  const addButtonRef = useRef<HTMLButtonElement>(null)
  const stripRef = useRef<HTMLDivElement>(null)
  const { draggingTabId, tabDropTarget } = useUiStore(useShallow((state) => ({ draggingTabId: state.draggingTabId, tabDropTarget: state.tabDropTarget })))
  const agents = useAgentStore((state) => state.agents)

  useEffect(() => {
    const strip = stripRef.current
    if (!strip) {
      return
    }
    const revealActiveTab = () => strip.querySelector(tabSelector(workspace.active))?.scrollIntoView({ block: 'nearest', inline: 'nearest' })
    const observer = new ResizeObserver(revealActiveTab)
    observer.observe(strip)
    return () => observer.disconnect()
  }, [workspace.active, workspace.tabs.length])

  const handleNewDefault = () => onNew(DEFAULT_SHELL)
  const handleContextMenu = (event: MouseEvent) => {
    event.preventDefault()
    setMenuOpen(true)
  }
  const handleAddKeyDown = (event: KeyboardEvent) => {
    if (isMenuKey(event)) {
      event.preventDefault()
      setMenuOpen(true)
    }
  }
  const handleCloseMenu = useCallback(() => {
    setMenuOpen(false)
    addButtonRef.current?.focus()
  }, [])
  const handleSelectShell = (shellId: string) => {
    setMenuOpen(false)
    onNew(shellId)
  }
  const tabMenuPosition = tabMenu ? workspace.tabs.findIndex((tab) => tab.id === tabMenu.tabId) : -1
  const tabMenuActions: TabMenuActions = { rename: onStartRename, shift: onShift, duplicate: onDuplicate, close: onClose, closeOthers: onCloseOthers }
  const handleRunTabMenu = () => setTabMenu(null)
  const handleDismissTabMenu = () => {
    const returnFocus = tabMenu?.returnFocus
    setTabMenu(null)
    if (returnFocus?.isConnected) {
      returnFocus.focus()
    }
  }
  const handleWheel = (event: WheelEvent<HTMLDivElement>) => {
    if (stripRef.current && event.deltaY !== 0) {
      stripRef.current.scrollLeft += event.deltaY
    }
  }
  const dropLine = (targeted: boolean) => `h-6 w-0.5 shrink-0 rounded ${targeted ? 'bg-dock-focus' : 'bg-transparent'}`

  return (
    <div data-drop-workspace={workspace.id} className="flex shrink-0 items-center gap-0.5 px-2 pt-1 select-none">
      <div ref={stripRef} role="tablist" className="flex min-w-0 items-center gap-0.5 overflow-x-auto [scrollbar-width:none]" onWheel={handleWheel}>
        {workspace.tabs.map((tab) => {
          const active = tab.id === workspace.active
          const targeted = isDropTarget(tabDropTarget, workspace.id, tab.id)
          const agentSummary = tabAgents(tab, agents)
          const handleSelect = () => onSelect(tab.id)
          const handleStartRename = () => onStartRename(tab.id)
          const handleClose = () => onClose(tab.id)
          const handleAuxClick = (event: MouseEvent) => {
            if (event.button === MIDDLE_BUTTON) {
              event.preventDefault()
              onClose(tab.id)
            }
          }
          const handlePointerDown = (event: PointerEvent<HTMLElement>) => beginTabDrag(event, tab.id, onMove)
          const handleTabContextMenu = (event: MouseEvent) => {
            event.preventDefault()
            setTabMenu({ tabId: tab.id, x: event.clientX, y: event.clientY, returnFocus: null })
          }
          const handleTabKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
            if (isMenuKey(event)) {
              event.preventDefault()
              const { left, bottom } = event.currentTarget.getBoundingClientRect()
              setTabMenu({ tabId: tab.id, x: left, y: bottom, returnFocus: event.currentTarget })
            }
          }
          return (
            <Fragment key={tab.id}>
              <span aria-hidden="true" className={dropLine(targeted)} />
              <div
                data-drop-workspace={workspace.id}
                data-drop-tab={tab.id}
                className={`flex min-w-[100px] items-center rounded-t-md border border-b-0 ${active ? 'border-dock-line bg-dock-panel text-dock-green-deep' : 'border-transparent text-dock-muted hover:bg-dock-green-hover'} ${draggingTabId === tab.id ? 'opacity-50' : ''}`}
                onAuxClick={handleAuxClick}
                onContextMenu={handleTabContextMenu}
              >
                {tab.id === renamingTabId ? (
                  <InlineNameEditor value={tab.name} label="Nom de l’onglet" className="mx-1 my-1 min-w-0 flex-1 text-xs" onCommit={onCommitRename} onCancel={onCancelRename} />
                ) : (
                  <button
                    type="button"
                    role="tab"
                    aria-selected={active}
                    data-tip="Double-clic pour renommer, glisser pour déplacer"
                    className="flex min-w-0 flex-1 cursor-pointer items-center gap-1.5 px-3 py-2 text-left text-xs"
                    onClick={handleSelect}
                    onDoubleClick={handleStartRename}
                    onPointerDown={handlePointerDown}
                    onKeyDown={handleTabKeyDown}
                  >
                    {agentSummary && <AgentStateIcon state={agentSummary.state} tip={agentSummary.tip} />}
                    <span className="min-w-0 truncate">{tab.name}</span>
                  </button>
                )}
                <button type="button" className="shrink-0 cursor-pointer px-2 text-xs hover:text-dock-error" data-tip="Fermer l’onglet" onClick={handleClose}>
                  ×
                </button>
              </div>
            </Fragment>
          )
        })}
        <span aria-hidden="true" className={dropLine(isDropTarget(tabDropTarget, workspace.id))} />
      </div>
      <div className="relative shrink-0">
        <button
          ref={addButtonRef}
          type="button"
          aria-haspopup="menu"
          aria-expanded={menuOpen}
          className="cursor-pointer rounded px-2 py-1 text-base hover:bg-dock-green-hover"
          data-tip="Nouvel onglet PowerShell (clic droit : choisir le shell)"
          onClick={handleNewDefault}
          onContextMenu={handleContextMenu}
          onKeyDown={handleAddKeyDown}
        >
          +
        </button>
        {menuOpen && <ShellMenu shells={shells} onSelect={handleSelectShell} onClose={handleCloseMenu} />}
      </div>
      <button
        type="button"
        aria-pressed={panelOpen}
        aria-label={panelOpen ? 'Masquer le panneau de droite' : 'Afficher le panneau de droite'}
        className={`ml-auto flex size-[26px] shrink-0 cursor-pointer items-center justify-center rounded-md hover:bg-dock-green-hover hover:text-dock-ink ${panelOpen ? 'text-dock-green-deep' : 'text-dock-muted'}`}
        data-tip={`${panelOpen ? 'Masquer' : 'Afficher'} le panneau Fichiers / Git (Ctrl + Maj + E ou G)`}
        onClick={onTogglePanel}
      >
        <Icon name={IconName.Explorer} size={14} />
      </button>
      {tabMenu && (
        <TabContextMenu
          request={tabMenu}
          position={tabMenuPosition}
          count={workspace.tabs.length}
          actions={tabMenuActions}
          onRun={handleRunTabMenu}
          onDismiss={handleDismissTabMenu}
        />
      )}
    </div>
  )
}
