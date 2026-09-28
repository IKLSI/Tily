import { memo, useEffect, useState } from 'react'
import { OpenTarget, type ShellProfile } from '../bridge/messages'
import { SplitAxis, type Pane } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useHostStore } from '../store/hostStore'
import { usePaneStore } from '../store/paneStore'
import { copyPaneBranch, copyPanePath, gitSummary, openPaneFolder, queryContext } from '../terminal/contextActions'
import { copyPaneSelection, focusPaneTerminal, hasPaneSelection, pasteIntoPane, selectAllInPane } from '../terminal/terminalActions'
import { TerminalPane } from '../terminal/TerminalPane'
import { AgentBadge } from './AgentBadge'
import { PaneOverlay } from './PaneOverlay'
import { TerminalContextMenu, type TerminalMenuActions, type TerminalMenuRequest } from './TerminalContextMenu'

interface PaneViewProps {
  pane: Pane
  active: boolean
  onFocus: (paneId: string) => void
  onClose: (paneId: string) => void
  onSplit: (paneId: string, axis: SplitAxis) => void
  shells: ShellProfile[]
  onRestart: (paneId: string) => void
  onRestartIn: (paneId: string, path: string) => void
  onChangeShell: (paneId: string, shellId: string) => void
  onDismissState: (paneId: string) => void
}

const HEADER_BUTTON = 'flex h-5 w-5 shrink-0 cursor-pointer items-center justify-center rounded hover:bg-dock-green-hover hover:text-dock-ink'
const MUTED_BUTTON = 'opacity-40 hover:bg-transparent hover:text-dock-muted'
const ICON_SIZE = 12
const ICON_PROPS = { width: ICON_SIZE, height: ICON_SIZE, viewBox: '0 0 12 12', fill: 'none', stroke: 'currentColor', strokeWidth: 1.2, strokeLinecap: 'round', strokeLinejoin: 'round' } as const

const CopyIcon = () => (
  <svg {...ICON_PROPS} aria-hidden="true">
    <rect x="4" y="4" width="6.5" height="6.5" rx="1" />
    <path d="M8 4V2.5A1 1 0 0 0 7 1.5H2.5a1 1 0 0 0-1 1V7a1 1 0 0 0 1 1H4" />
  </svg>
)

const EditorIcon = () => (
  <svg {...ICON_PROPS} aria-hidden="true">
    <path d="M4 3 1.5 6 4 9" />
    <path d="M8 3l2.5 3L8 9" />
  </svg>
)

const FolderIcon = () => (
  <svg {...ICON_PROPS} aria-hidden="true">
    <path d="M1.5 3.5h3l1 1h5v5.5h-9z" />
  </svg>
)

const BranchIcon = () => (
  <svg {...ICON_PROPS} aria-hidden="true">
    <circle cx="3" cy="2.5" r="1.2" />
    <circle cx="3" cy="9.5" r="1.2" />
    <circle cx="9" cy="4" r="1.2" />
    <path d="M3 3.7v4.6M9 5.2c0 2-6 1.5-6 3.5" />
  </svg>
)

const SplitIcon = ({ horizontal }: { horizontal: boolean }) => (
  <svg {...ICON_PROPS} aria-hidden="true">
    <rect x="1.5" y="1.5" width="9" height="9" rx="1.5" />
    {horizontal ? <line x1="6" y1="1.5" x2="6" y2="10.5" /> : <line x1="1.5" y1="6" x2="10.5" y2="6" />}
  </svg>
)

const CloseIcon = () => (
  <svg {...ICON_PROPS} aria-hidden="true">
    <line x1="3" y1="3" x2="9" y2="9" />
    <line x1="9" y1="3" x2="3" y2="9" />
  </svg>
)

export const PaneView = memo(function PaneView({ pane, active, onFocus, onClose, onSplit, shells, onRestart, onRestartIn, onChangeShell, onDismissState }: PaneViewProps) {
  const paneState = usePaneStore((state) => state.states[pane.id])
  const context = useHostStore((state) => state.contexts[pane.id])
  const agent = useAgentStore((state) => state.agents[pane.id])
  const [menu, setMenu] = useState<TerminalMenuRequest | null>(null)

  useEffect(() => {
    queryContext(pane.id)
  }, [pane.id, pane.path])

  const handleHeaderMouseDown = () => onFocus(pane.id)
  const handleRestart = () => onRestart(pane.id)
  const handleRestartIn = (path: string) => onRestartIn(pane.id, path)
  const handleDismissState = () => onDismissState(pane.id)
  const handleChangeShell = (shellId: string) => onChangeShell(pane.id, shellId)
  const handleCopyPath = () => copyPanePath(pane.id)
  const handleOpenEditor = () => openPaneFolder(pane.id, OpenTarget.Editor)
  const handleOpenExplorer = () => openPaneFolder(pane.id, OpenTarget.Explorer)
  const handleCopyBranch = () => copyPaneBranch(pane.id)
  const handleSplitSideBySide = () => onSplit(pane.id, SplitAxis.Horizontal)
  const handleSplitTopBottom = () => onSplit(pane.id, SplitAxis.Vertical)
  const handleClose = () => onClose(pane.id)
  const handleContextMenu = (x: number, y: number) => setMenu({ x, y })
  const handleDismissMenu = () => {
    setMenu(null)
    focusPaneTerminal(pane.id)
  }
  const menuActions: TerminalMenuActions = {
    copy: () => copyPaneSelection(pane.id),
    paste: () => pasteIntoPane(pane.id),
    selectAll: () => selectAllInPane(pane.id),
    splitSideBySide: handleSplitSideBySide,
    splitTopBottom: handleSplitTopBottom,
    close: handleClose,
  }
  const branchTitle = context?.branch ? `Copier la branche « ${context.branch} »` : gitSummary(context)

  return (
    <section
      data-pane-id={pane.id}
      className={`grid h-full min-h-0 grid-rows-[24px_1fr] overflow-hidden rounded-md border bg-dock-terminal ${active ? 'border-dock-green' : 'border-dock-line'}`}
    >
      <header
        className="flex items-center gap-1 bg-dock-panel px-2 text-[11px] text-dock-muted select-none"
        onMouseDown={handleHeaderMouseDown}
      >
        <span className="mr-1 font-semibold text-dock-ink">{pane.shell}</span>
        {agent && <AgentBadge agent={agent} />}
        <span className="min-w-0 flex-1 truncate font-mono text-dock-green" data-tip={pane.path}>
          {pane.path}
        </span>
        <button type="button" className={HEADER_BUTTON} data-tip={`Copier le chemin ${pane.path}`} aria-label="Copier le chemin" onClick={handleCopyPath}>
          <CopyIcon />
        </button>
        <button type="button" className={HEADER_BUTTON} data-tip="Ouvrir dans l’éditeur" aria-label="Ouvrir dans l’éditeur" onClick={handleOpenEditor}>
          <EditorIcon />
        </button>
        <button type="button" className={HEADER_BUTTON} data-tip="Ouvrir dans l’explorateur" aria-label="Ouvrir dans l’explorateur" onClick={handleOpenExplorer}>
          <FolderIcon />
        </button>
        <button type="button" className={`${HEADER_BUTTON} ${context?.branch ? '' : MUTED_BUTTON}`} data-tip={branchTitle} aria-label="Copier la branche Git" aria-disabled={!context?.branch} onClick={handleCopyBranch}>
          <BranchIcon />
        </button>
        <span className="mx-1 h-3 w-px bg-dock-line" aria-hidden="true" />
        <button type="button" className={HEADER_BUTTON} data-tip="Split côte à côte" aria-label="Split côte à côte" onClick={handleSplitSideBySide}>
          <SplitIcon horizontal />
        </button>
        <button type="button" className={HEADER_BUTTON} data-tip="Split haut / bas" aria-label="Split haut / bas" onClick={handleSplitTopBottom}>
          <SplitIcon horizontal={false} />
        </button>
        <button type="button" className={`${HEADER_BUTTON} hover:text-dock-error`} data-tip="Fermer le pane" aria-label="Fermer le pane" onClick={handleClose}>
          <CloseIcon />
        </button>
      </header>
      <div className="relative min-h-0">
        <TerminalPane pane={pane} active={active} onFocus={onFocus} onContextMenu={handleContextMenu} />
        {paneState && <PaneOverlay state={paneState} shells={shells} onRestart={handleRestart} onRestartIn={handleRestartIn} onChangeShell={handleChangeShell} onDismiss={handleDismissState} onClose={handleClose} />}
      </div>
      {menu && <TerminalContextMenu request={menu} canCopy={hasPaneSelection(pane.id)} actions={menuActions} onDismiss={handleDismissMenu} />}
    </section>
  )
})
