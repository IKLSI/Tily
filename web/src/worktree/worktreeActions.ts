import { bridge } from '../bridge/bridge'
import { WorktreeBranchMode, WorktreeOperation } from '../bridge/worktreeMessages'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { activePane, activeTab, activeWorkspace, DEFAULT_SHELL, panesOf } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { useWorktreeStore, WorktreePickerKind, type WorktreeDraft, type WorktreeRemoval } from '../store/worktreeStore'
import { requestClose } from '../terminal/closeGuard'
import { joinPane } from '../terminal/terminalActions'
import { terminalRegistry } from '../terminal/terminalRegistry'
import { panesWithin, removalPanes, worktreeTarget } from './worktreePaths'

const PLAN_DELAY_MS = 250

let planTimer: ReturnType<typeof setTimeout> | undefined

type DraftPreset = Partial<Pick<WorktreeDraft, 'branch' | 'mode' | 'base'>>

const sendPlan = (request: number): void => {
  const { draft, planRequest } = useWorktreeStore.getState()
  if (!draft || request !== planRequest) {
    return
  }
  bridge.send({ type: 'worktrees.plan', request, repository: draft.repository, branch: draft.branch, mode: draft.mode, base: draft.base || undefined })
}

const schedulePlan = (delay: number): void => {
  clearTimeout(planTimer)
  const request = useWorktreeStore.getState().requestPlan()
  planTimer = setTimeout(() => sendPlan(request), delay)
}

export const openWorktreeDialog = (repository: string, preset: DraftPreset = {}): void => {
  const { setPicker, setDraft, setCreateFailure } = useWorktreeStore.getState()
  setPicker(null)
  setCreateFailure(null)
  setDraft({ repository, branch: '', mode: WorktreeBranchMode.New, base: '', install: true, database: true, ...preset })
  schedulePlan(0)
}

export const changeWorktreeDraft = (patch: Partial<WorktreeDraft>): void => {
  const { draft, setDraft } = useWorktreeStore.getState()
  if (!draft) {
    return
  }
  setDraft({ ...draft, ...patch })
  if ('branch' in patch || 'mode' in patch || 'base' in patch || 'repository' in patch) {
    schedulePlan(PLAN_DELAY_MS)
  }
}

export const closeWorktreeDialog = (): void => {
  clearTimeout(planTimer)
  useWorktreeStore.getState().setDraft(null)
  focusActivePane()
}

export const submitWorktree = (): void => {
  const { draft, plan, planPending, busy, setBusy, setCreateFailure } = useWorktreeStore.getState()
  if (!draft || !plan?.path || plan.error || planPending || busy) {
    return
  }
  setCreateFailure(null)
  setBusy(WorktreeOperation.Create)
  useHostStore.getState().setStatus('Création du worktree…')
  bridge.send({ type: 'worktrees.create', repository: draft.repository, branch: draft.branch, mode: draft.mode, base: draft.base || undefined, install: draft.install, database: draft.database })
}

export const openWorktreePicker = (kind: WorktreePickerKind): void => {
  bridge.send({ type: 'projects.list' })
  useWorktreeStore.getState().setPicker(kind)
}

export const closeWorktreePicker = (): void => {
  useWorktreeStore.getState().setPicker(null)
  focusActivePane()
}

export const startWorktreeCreation = (): void => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  const pane = workspace ? activePane(activeTab(workspace)) : undefined
  if (pane && useHostStore.getState().contexts[pane.id]?.isRepository) {
    openWorktreeDialog(pane.path)
  } else {
    openWorktreePicker(WorktreePickerKind.Source)
  }
}

export const openWorktree = (path: string, inActiveWorkspace = false): void => {
  const { session, newWorkspace, newTabAt } = useSessionStore.getState()
  const existing = session ? panesWithin(session, path)[0] : undefined
  if (existing) {
    joinPane(existing.id)
    return
  }
  if (inActiveWorkspace && session && activeWorkspace(session)) {
    newTabAt(path, DEFAULT_SHELL)
  } else {
    newWorkspace(worktreeTarget(path).name, path, DEFAULT_SHELL)
  }
}

export const openCreatedWorktree = (path: string, name: string, install: string | undefined): void => {
  const { newWorkspace } = useSessionStore.getState()
  const workspaceId = newWorkspace(name, path, DEFAULT_SHELL)
  const workspace = useSessionStore.getState().session?.workspaces.find((candidate) => candidate.id === workspaceId)
  if (workspace && install) {
    terminalRegistry.runAtStart(activeTab(workspace).active, install)
  }
}

export const requestWorktreeRemoval = (path: string, branch?: string): void => {
  const { session } = useSessionStore.getState()
  if (!session) {
    return
  }
  const panes = removalPanes(session, path, useAgentStore.getState().agents)
  useWorktreeStore.getState().setRemoval({ ...worktreeTarget(path), branch, panes, closePanes: true, keepBranch: false, dropDatabase: true, failure: null })
}

export const changeWorktreeRemoval = (patch: Partial<WorktreeRemoval>): void => {
  const { removal, setRemoval } = useWorktreeStore.getState()
  if (removal) {
    setRemoval({ ...removal, ...patch })
  }
}

export const cancelWorktreeRemoval = (): void => {
  useWorktreeStore.getState().setRemoval(null)
  focusActivePane()
}

const closePanesNow = (paneIds: string[]): void => {
  const targets = new Set(paneIds)
  const { session, closeTab, closePane } = useSessionStore.getState()
  for (const tab of session?.workspaces.flatMap((workspace) => workspace.tabs) ?? []) {
    const panes = panesOf(tab.tree)
    if (panes.every((pane) => targets.has(pane.id))) {
      closeTab(tab.id)
    } else {
      panes.filter((pane) => targets.has(pane.id)).forEach((pane) => closePane(pane.id))
    }
  }
}

export const confirmWorktreeRemoval = (): void => {
  const { removal, busy, setRemoval } = useWorktreeStore.getState()
  if (!removal || busy) {
    return
  }
  setRemoval(null)
  const paneIds = removal.closePanes ? removal.panes.map((pane) => pane.paneId) : []
  requestClose(`Supprimer le worktree « ${removal.name} » ?`, paneIds, () => {
    closePanesNow(paneIds)
    const store = useWorktreeStore.getState()
    store.setPendingRemoval({ ...removal, panes: removal.closePanes ? [] : removal.panes, failure: null })
    store.setBusy(WorktreeOperation.Remove)
    useHostStore.getState().setStatus('Suppression du worktree…')
    bridge.send({ type: 'worktrees.remove', path: removal.path, keepBranch: removal.keepBranch, dropDatabase: removal.dropDatabase, confirmed: true })
  })
}
