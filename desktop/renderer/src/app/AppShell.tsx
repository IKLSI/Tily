import { lazy, Suspense, useEffect, useMemo } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { bridge } from '../bridge/bridge'
import { PickTarget, type AttentionKind, type NotificationSettings, type Settings } from '../bridge/messages'
import { activePane, activeTab, activeWorkspace, DEFAULT_SHELL, EXPLORER_DEFAULT, EXPLORER_MAX, EXPLORER_MIN, findWorkspace, RightPanelView, SIDEBAR_DEFAULT, SIDEBAR_MAX, SIDEBAR_MIN, type Session, type SplitAxis, type SplitPath, type Workspace } from '../model/session'
import { toggleFavoriteCommand, type PaletteItem } from '../features/palette/paletteItems'
import { waitingPanes } from '../features/agents/agentSummary'
import { Command, handleDocumentShortcut, handleLeaderKeyCapture, runCommand } from '../features/keyboard/shortcuts'
import { agentKey, useAgentStore } from '../features/agents/agentStore'
import { useHostStore } from '../stores/hostStore'
import { useSessionStore } from '../stores/sessionStore'
import { RenameOrigin, useUiStore } from '../stores/uiStore'
import { cancelClose, confirmClose } from '../features/terminal/closeGuard'
import { confirmDelete, focusFileTree } from '../features/explorer/fileExplorerActions'
import { useExplorerStore } from '../features/explorer/explorerStore'
import { focusGitPanel, takeFocusFromCoveredTerminals } from '../features/git/gitFocus'
import { openWorkspaceNotes, toggleRightPanel } from '../features/right-panel/rightPanel'
import { useGitStore } from '../features/git/gitStore'
import { usePreviewStore } from '../features/preview/previewStore'
import { useMcpConsentStore } from '../features/mcp/mcpConsentStore'
import { usePasteStore } from '../features/terminal/pasteStore'
import { worktreeModalOpen } from '../features/worktrees/worktreeStore'
import { filePickerOpen } from '../features/explorer/filePickerStore'
import { commitPickerOpen } from '../features/git/commitPickerStore'
import { changePaneShell, dismissPaneState, restartPane, restartPaneIn } from '../features/terminal/paneLifecycle'
import { closeOtherTabsKeepingText, closePaneKeepingText, closeTabKeepingText, closeWorkspaceKeepingText, duplicateTabKeepingLayout, restoreClosedTab } from '../features/terminal/tabLifecycle'
import { focusPane, joinPane } from '../features/terminal/terminalActions'
import { togglePaneZoom, useEndZoomWhenPaneChanges, zoomedPaneOf } from '../features/terminal/paneZoom'
import { AttentionToasts } from '../features/agents/components/AttentionToasts'
import { CloseConfirmDialog } from '../features/terminal/components/CloseConfirmDialog'
import { McpConsentDialog } from '../features/mcp/components/McpConsentDialog'
import { PasteConfirmDialog } from '../features/terminal/components/PasteConfirmDialog'
import { CommandPalette } from '../features/palette/components/CommandPalette'
import { DeleteConfirmDialog } from '../components/DeleteConfirmDialog'
import { ErrorBoundary } from '../components/ErrorBoundary'
import { recoverFromDialogError } from './dialogRecovery'
import { EmptyState } from './EmptyState'
import { LazyFilePreview } from '../features/preview/components/LazyFilePreview'
import { UnsavedPreviewDialog } from '../features/preview/components/UnsavedPreviewDialog'
import { GitConfirmDialog, GitContextMenu, GitDiffDrawer, GitGraphView } from './lazyViews'
import { Header } from './Header'
import { HeaderWorkspaces } from '../features/workspaces/components/HeaderWorkspaces'
import { FilePicker } from '../features/explorer/components/FilePicker'
import { CommitPicker } from '../features/git/components/CommitPicker'
import { RightPanel } from '../features/right-panel/components/RightPanel'
import { SidebarResizer } from './SidebarResizer'
import { SplitView } from '../features/terminal/components/SplitView'
import { StatusBar } from '../features/status-log/components/StatusBar'
import { TabBar } from '../features/workspaces/components/TabBar'
import { Tooltip } from '../components/Tooltip'
import type { WorkspacePanelActions } from '../features/workspaces/components/workspacePanel'
import type { HeaderWorkspaceActions } from '../features/workspaces/components/workspaceStrip'
import { WorkspaceTree } from '../features/workspaces/components/WorkspaceTree'
import { WorktreeDialogs } from '../features/worktrees/components/WorktreeDialogs'

const ProjectPickers = lazy(() => import('../features/projects/components/ProjectPickers').then((module) => ({ default: module.ProjectPickers })))
const SettingsDialog = lazy(() => import('../features/settings/components/SettingsDialog').then((module) => ({ default: module.SettingsDialog })))

const OVERLAY_FALLBACK = 'absolute bottom-10 left-1/2 z-50 flex -translate-x-1/2 flex-col items-center gap-3 rounded border border-tily-line bg-tily-panel p-4 text-center shadow-lg'
const PANEL_FALLBACK = 'flex w-72 shrink-0 flex-col items-center justify-center gap-3 border-l border-tily-line bg-tily-panel p-4 text-center'

interface AppShellProps {
  session: Session
}

const currentWorkspace = (): Workspace | undefined => {
  const { session } = useSessionStore.getState()
  return session ? activeWorkspace(session) : undefined
}

const focusActivePane = (): void => {
  const workspace = currentWorkspace()
  if (workspace) {
    focusPane(activeTab(workspace).active)
  }
}

const finishRename = (): void => {
  useUiStore.getState().stopRenamingWorkspace()
  focusActivePane()
}

const handleCommitRename = (name: string): void => {
  const { renamingWorkspaceId } = useUiStore.getState()
  if (renamingWorkspaceId) {
    useSessionStore.getState().renameWorkspace(renamingWorkspaceId, name)
  }
  finishRename()
}

const handleStartRenameFromPanel = (workspaceId: string): void => useUiStore.getState().startRenamingWorkspace(workspaceId, RenameOrigin.Panel)

const finishTabRename = (): void => {
  useUiStore.getState().stopRenamingTab()
  focusActivePane()
}

const handleCommitTabRename = (name: string): void => {
  const { renamingTabId } = useUiStore.getState()
  if (renamingTabId) {
    useSessionStore.getState().renameTab(renamingTabId, name)
  }
  finishTabRename()
}

const handleSelectTab = (workspaceId: string, tabId: string): void => {
  const { selectWorkspace, selectTab } = useSessionStore.getState()
  selectWorkspace(workspaceId)
  selectTab(tabId)
  const { session } = useSessionStore.getState()
  const target = session ? findWorkspace(session, workspaceId)?.tabs.find((candidate) => candidate.id === tabId) : undefined
  if (target) {
    focusPane(target.active)
  }
}

const handleSplit = (paneId: string, axis: SplitAxis): void => {
  const { selectPane, splitPane } = useSessionStore.getState()
  selectPane(paneId)
  splitPane(axis)
}

const handleJoinPane = (paneId: string): void => joinPane(paneId)

const handleSelectWorkspace = (workspaceId: string): void => {
  useSessionStore.getState().selectWorkspace(workspaceId)
  focusActivePane()
}

const headerActions: HeaderWorkspaceActions = {
  select: handleSelectWorkspace,
  joinPane: handleJoinPane,
  startRename: (workspaceId) => useUiStore.getState().startRenamingWorkspace(workspaceId, RenameOrigin.Header),
  commitRename: handleCommitRename,
  cancelRename: finishRename,
}

const handleNewWorkspace = (): void => {
  const { session, newWorkspace } = useSessionStore.getState()
  const workspaceId = newWorkspace(`Workspace ${(session?.workspaces.length ?? 0) + 1}`, useHostStore.getState().home, DEFAULT_SHELL)
  useUiStore.getState().startRenamingWorkspace(workspaceId, RenameOrigin.Panel)
}

const handleOpenProjects = (): void => runCommand(Command.Projects)

const handleOpenSettings = (): void => {
  bridge.send({ type: 'settings.get' })
  useUiStore.getState().openSettings()
}

const handleToggleSidebar = (): void => {
  const { session, toggleSidebar } = useSessionStore.getState()
  if (session && !session.sidebarCollapsed && document.activeElement?.closest('aside')) {
    focusActivePane()
  }
  toggleSidebar()
}

const handleStartRenameFromHeader = (): void => {
  const workspace = currentWorkspace()
  if (workspace) {
    useUiStore.getState().startRenamingWorkspace(workspace.id, RenameOrigin.Header)
  }
}

const handleNewTabIn = (workspaceId: string): void => {
  const { selectWorkspace, newTab } = useSessionStore.getState()
  selectWorkspace(workspaceId)
  newTab(DEFAULT_SHELL)
}

const handleOpenTerminalAt = (path: string): void => {
  const { session, newTabAt } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  newTabAt(path, workspace ? activePane(activeTab(workspace)).shell : DEFAULT_SHELL)
}

const handleCancelDelete = (): void => {
  useExplorerStore.getState().cancelDelete()
  focusFileTree()
}

const handleConfirmGit = (): void => {
  const { confirmation, confirm } = useGitStore.getState()
  confirm(null)
  confirmation?.run()
  requestAnimationFrame(confirmation?.restoreFocus ?? focusGitPanel)
}

const handleCancelGit = (): void => {
  const { confirmation, confirm } = useGitStore.getState()
  const restoreFocus = confirmation?.restoreFocus ?? focusGitPanel
  confirm(null)
  restoreFocus()
}

const handleDialogError = (): void => {
  recoverFromDialogError()
  requestAnimationFrame(focusActivePane)
}

const confirmationOpen = (): boolean => {
  const { settingsOpen, closeConfirmation } = useUiStore.getState()
  return settingsOpen || closeConfirmation !== null || useExplorerStore.getState().deleteRequest !== null || useGitStore.getState().confirmation !== null || usePasteStore.getState().request !== null || useMcpConsentStore.getState().queue.length > 0 || worktreeModalOpen() || filePickerOpen() || commitPickerOpen() || usePreviewStore.getState().pendingAction !== null
}

const modalOpen = (): boolean => {
  const { paletteOpen, projectPickerOpen } = useUiStore.getState()
  return paletteOpen || projectPickerOpen || confirmationOpen()
}

const panelActions: WorkspacePanelActions = {
  selectWorkspace: (workspaceId) => useSessionStore.getState().selectWorkspace(workspaceId),
  toggleWorkspace: (workspaceId) => useSessionStore.getState().toggleWorkspace(workspaceId),
  startRenameWorkspace: handleStartRenameFromPanel,
  commitRenameWorkspace: handleCommitRename,
  cancelRenameWorkspace: finishRename,
  closeWorkspace: closeWorkspaceKeepingText,
  openNotes: openWorkspaceNotes,
  newTabIn: handleNewTabIn,
  collapseOthers: (workspaceId) => useSessionStore.getState().collapseOtherWorkspaces(workspaceId),
  moveWorkspace: (workspaceId, offset) => useSessionStore.getState().moveWorkspace(workspaceId, offset),
  moveWorkspaceBefore: (workspaceId, beforeWorkspaceId) => useSessionStore.getState().moveWorkspaceBefore(workspaceId, beforeWorkspaceId),
  shiftTab: (tabId, offset) => useSessionStore.getState().shiftTab(tabId, offset),
  duplicateTab: duplicateTabKeepingLayout,
  selectTab: handleSelectTab,
  startRenameTab: (tabId) => useUiStore.getState().startRenamingTab(tabId, RenameOrigin.Panel),
  commitRenameTab: handleCommitTabRename,
  cancelRenameTab: finishTabRename,
  closeTab: closeTabKeepingText,
  moveTab: (tabId, workspaceId, beforeTabId) => useSessionStore.getState().moveTab(tabId, workspaceId, beforeTabId),
  joinPane: handleJoinPane,
  newWorkspace: handleNewWorkspace,
  openProjects: handleOpenProjects,
}

export function AppShell({ session }: AppShellProps) {
  const { selectTab, selectPane, setSidebarWidth, setExplorerWidth, newTab, moveTab, shiftTab, setSplitRatio } = useSessionStore.getState()
  const { leaderActive, shells, settingsSnapshot, pickedPath, importedPreferences } = useHostStore(
    useShallow((state) => ({
      leaderActive: state.leaderActive,
      shells: state.shells,
      settingsSnapshot: state.settingsSnapshot,
      pickedPath: state.pickedPath,
      importedPreferences: state.importedPreferences,
    })),
  )
  const { renamingWorkspaceId, renameOrigin, renamingTabId, tabRenameOrigin, paletteOpen, projectPickerOpen, settingsOpen, closeConfirmation, zoomedPaneId } = useUiStore(
    useShallow((state) => ({
      zoomedPaneId: state.zoomedPaneId,
      renamingWorkspaceId: state.renamingWorkspaceId,
      renameOrigin: state.renameOrigin,
      renamingTabId: state.renamingTabId,
      tabRenameOrigin: state.tabRenameOrigin,
      paletteOpen: state.paletteOpen,
      projectPickerOpen: state.projectPickerOpen,
      settingsOpen: state.settingsOpen,
      closeConfirmation: state.closeConfirmation,
    })),
  )
  const { startRenamingTab, openPalette, closePalette, closeSettings } = useUiStore.getState()
  const deleteRequest = useExplorerStore((state) => state.deleteRequest)
  const gitConfirmation = useGitStore((state) => state.confirmation)
  const unsavedPreview = usePreviewStore((state) => state.pendingAction !== null)
  const gitGraphReady = useGitStore((state) => state.graphOpen && state.state !== null)
  const agents = useAgentStore((state) => state.agents)
  const acknowledged = useAgentStore((state) => state.acknowledged)
  const waiting = useMemo(() => waitingPanes(session, agents).filter((pane) => acknowledged[pane.paneId] !== agentKey(pane.agent)), [session, agents, acknowledged])
  const workspace = activeWorkspace(session)
  const tab = workspace ? activeTab(workspace) : undefined
  const panelView = tab?.panel ?? RightPanelView.Files
  const gitShown = Boolean(tab?.explorer) && panelView === RightPanelView.Git
  const filesShown = Boolean(tab?.explorer) && panelView === RightPanelView.Files
  const graphShown = gitShown && gitGraphReady
  const tabId = tab?.id
  const activePaneId = tab?.active

  useEndZoomWhenPaneChanges(zoomedPaneId, tab)
  const availableShells = useMemo(() => shells.filter((shell) => shell.available), [shells])

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!event.defaultPrevented && event.ctrlKey && !event.altKey && event.key.toLowerCase() === 'p' && !confirmationOpen()) {
        event.preventDefault()
        openPalette()
      }
      if (!modalOpen()) {
        handleDocumentShortcut(event)
      }
    }
    document.addEventListener('keydown', handleLeaderKeyCapture, true)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleLeaderKeyCapture, true)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [openPalette])

  useEffect(() => {
    if (graphShown) {
      takeFocusFromCoveredTerminals()
    }
  }, [graphShown, tabId, activePaneId, zoomedPaneId])

  const handleClosePalette = () => {
    closePalette()
    focusActivePane()
    if (graphShown) {
      takeFocusFromCoveredTerminals()
    }
  }
  const handleRunPaletteItem = (item: PaletteItem) => {
    handleClosePalette()
    item.run()
  }
  const handleCloseSettings = () => {
    closeSettings()
    focusActivePane()
  }
  const handleSaveSettings = (settings: Settings) => bridge.send({ type: 'settings.save', settings })
  const handleCancelClose = () => {
    cancelClose()
    focusActivePane()
  }
  const handlePickPath = (field: string, target: PickTarget) => bridge.send({ type: 'dialog.pick', field, target })
  const handleExportPreferences = () => bridge.send({ type: 'settings.export' })
  const handleImportPreferences = () => bridge.send({ type: 'settings.import' })
  const handleInstallHooks = () => bridge.send({ type: 'agents.installHooks' })
  const handleRemoveHooks = () => bridge.send({ type: 'agents.removeHooks' })
  const handleTestNotification = (notifications: NotificationSettings, kind: AttentionKind) => bridge.send({ type: 'attention.test', pane: tab?.active ?? '', kind, notifications })
  const handleDismissAttention = (paneId: string) => useAgentStore.getState().acknowledge(paneId)

  const renderMain = (current: Workspace) => {
    const currentTab = activeTab(current)
    const zoomedPane = zoomedPaneOf(currentTab, zoomedPaneId)
    const handleResize = (path: SplitPath, ratio: number) => setSplitRatio(currentTab.id, path, ratio)
    return (
      <>
        <TabBar
          workspace={current}
          shells={availableShells}
          renamingTabId={tabRenameOrigin === RenameOrigin.TabBar ? renamingTabId : null}
          panelOpen={Boolean(currentTab.explorer)}
          onTogglePanel={toggleRightPanel}
          onSelect={selectTab}
          onStartRename={startRenamingTab}
          onCommitRename={handleCommitTabRename}
          onCancelRename={finishTabRename}
          onClose={closeTabKeepingText}
          onCloseOthers={closeOtherTabsKeepingText}
          onShift={shiftTab}
          onDuplicate={duplicateTabKeepingLayout}
          onNew={newTab}
          onMove={moveTab}
        />
        <div className="relative min-h-0 flex-1 border-t border-tily-line bg-tily-panel p-1">
          <ErrorBoundary resetKey={currentTab.id}>
            <SplitView key={currentTab.id} node={zoomedPane ? { pane: zoomedPane } : currentTab.tree} zoomed={zoomedPane !== undefined} onToggleZoom={togglePaneZoom} activePaneId={currentTab.active} onFocus={selectPane} onClose={closePaneKeepingText} onSplit={handleSplit} onResize={handleResize} shells={availableShells} onRestart={restartPane} onRestartIn={restartPaneIn} onChangeShell={changePaneShell} onDismissState={dismissPaneState} />
          </ErrorBoundary>
          <ErrorBoundary resetKey={currentTab.id} className={OVERLAY_FALLBACK}>
            <Suspense fallback={null}>
              {graphShown && <GitGraphView layout={session.gitGraph} />}
              {gitShown && <GitDiffDrawer />}
              {gitShown && <GitContextMenu />}
            </Suspense>
            {filesShown && <LazyFilePreview />}
          </ErrorBoundary>
        </div>
      </>
    )
  }

  return (
    <div className="relative flex h-full flex-col">
      <Header
        workspaceName={workspace?.name ?? null}
        renaming={workspace !== undefined && renamingWorkspaceId === workspace.id && renameOrigin === RenameOrigin.Header}
        sidebarCollapsed={session.sidebarCollapsed}
        leaderActive={leaderActive}
        navigation={
          session.sidebarCollapsed && workspace ? (
            <HeaderWorkspaces workspaces={session.workspaces} activeId={session.active} renamingId={renameOrigin === RenameOrigin.Header ? renamingWorkspaceId : null} actions={headerActions} />
          ) : null
        }
        onToggleSidebar={handleToggleSidebar}
        onOpenSettings={handleOpenSettings}
        onStartRename={handleStartRenameFromHeader}
        onCommitRename={handleCommitRename}
        onCancelRename={finishRename}
      />
      <div className="flex min-h-0 flex-1">
        {!session.sidebarCollapsed && (
          <>
            <WorkspaceTree
              session={session}
              renamingWorkspaceId={renameOrigin === RenameOrigin.Panel ? renamingWorkspaceId : null}
              renamingTabId={tabRenameOrigin === RenameOrigin.Panel ? renamingTabId : null}
              actions={panelActions}
            />
            <SidebarResizer width={session.sidebar} min={SIDEBAR_MIN} max={SIDEBAR_MAX} defaultWidth={SIDEBAR_DEFAULT} label="Largeur du panneau des workspaces" onResize={setSidebarWidth} />
          </>
        )}
        <main className="relative flex min-h-0 min-w-0 flex-1 flex-col">
          {workspace ? renderMain(workspace) : <EmptyState canRestore={session.closed.length > 0} onNewWorkspace={handleNewWorkspace} onOpenProject={handleOpenProjects} onRestoreTab={restoreClosedTab} />}
        </main>
        {tab?.explorer && (
          <>
            <SidebarResizer width={session.explorerWidth} min={EXPLORER_MIN} max={EXPLORER_MAX} defaultWidth={EXPLORER_DEFAULT} label="Largeur du panneau de droite" reversed onResize={setExplorerWidth} />
            <ErrorBoundary resetKey={panelView} className={PANEL_FALLBACK}>
              <RightPanel view={panelView} root={activePane(tab).path} width={session.explorerWidth} onClose={toggleRightPanel} onOpenTerminal={handleOpenTerminalAt} />
            </ErrorBoundary>
          </>
        )}
      </div>
      <AttentionToasts waiting={waiting} onJoin={handleJoinPane} onDismiss={handleDismissAttention} />
      <ErrorBoundary className={OVERLAY_FALLBACK} onRecover={handleDialogError}>
        <FilePicker />
        <CommitPicker />
        {projectPickerOpen && (
          <Suspense fallback={null}>
            <ProjectPickers />
          </Suspense>
        )}
        {settingsOpen && (
          <Suspense fallback={null}>
            <SettingsDialog snapshot={settingsSnapshot} pickedPath={pickedPath} imported={importedPreferences} onClose={handleCloseSettings} onSave={handleSaveSettings} onPick={handlePickPath} onExport={handleExportPreferences} onImport={handleImportPreferences} onInstallHooks={handleInstallHooks} onRemoveHooks={handleRemoveHooks} onTestNotification={handleTestNotification} />
          </Suspense>
        )}
        {paletteOpen && <CommandPalette session={session} shells={availableShells} onClose={handleClosePalette} onRun={handleRunPaletteItem} onToggleFavorite={toggleFavoriteCommand} />}
        {deleteRequest && <DeleteConfirmDialog request={deleteRequest} onConfirm={confirmDelete} onCancel={handleCancelDelete} />}
        <WorktreeDialogs />
        {gitConfirmation && (
          <Suspense fallback={null}>
            <GitConfirmDialog confirmation={gitConfirmation} onConfirm={handleConfirmGit} onCancel={handleCancelGit} />
          </Suspense>
        )}
        {closeConfirmation && <CloseConfirmDialog confirmation={closeConfirmation} onConfirm={confirmClose} onCancel={handleCancelClose} />}
        <PasteConfirmDialog />
        {unsavedPreview && <UnsavedPreviewDialog />}
        <McpConsentDialog />
      </ErrorBoundary>
      <Tooltip />
      <StatusBar />
    </div>
  )
}
