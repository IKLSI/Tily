import { WorktreeOperation, type WorktreePlan } from '../bridge/worktreeMessages'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useWorktreeStore } from '../store/worktreeStore'
import { openCreatedWorktree } from './worktreeActions'

export const receiveWorktreePlan = (request: number, plan: WorktreePlan): void => useWorktreeStore.getState().receivePlan(request, plan)

export const receiveWorktreeProgress = (operation: WorktreeOperation, message: string): void => {
  useWorktreeStore.getState().setBusy(operation)
  useHostStore.getState().setStatus(message)
}

export const receiveWorktreeCreated = (path: string, name: string, install: string | undefined): void => {
  useWorktreeStore.getState().setDraft(null)
  openCreatedWorktree(path, name, install)
  useHostStore.getState().setStatus(install ? `Worktree « ${name} » ouvert : ${install} tourne dans son terminal.` : `Worktree « ${name} » ouvert.`)
}

export const receiveWorktreeDone = (operation: WorktreeOperation, message: string, warnings: string[]): void => {
  const store = useWorktreeStore.getState()
  store.setBusy(null)
  if (operation === WorktreeOperation.Remove) {
    store.setPendingRemoval(null)
  }
  const warned = warnings.length > 0
  useHostStore.getState().setStatus(warned ? `${message} ${warnings.join(' ')}` : message, warned ? StatusLevel.Warning : StatusLevel.Info)
}

export const receiveWorktreeFailed = (operation: WorktreeOperation, message: string, output: string | undefined, lockedBy: string[] | undefined): void => {
  const store = useWorktreeStore.getState()
  store.setBusy(null)
  const failure = { message, output, lockedBy }
  if (operation === WorktreeOperation.Remove && store.pendingRemoval) {
    store.setRemoval({ ...store.pendingRemoval, failure })
    store.setPendingRemoval(null)
  } else if (operation === WorktreeOperation.Create && store.draft) {
    store.setCreateFailure(failure)
  }
  useHostStore.getState().setStatus(message, StatusLevel.Error)
}
