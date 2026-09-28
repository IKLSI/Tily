import { GitDiffSource, type GitCommitDetails, type GitDiff, type GitFailureCode, type GitHistory, type GitState } from '../bridge/gitMessages'
import { useGitStore, type GitFileTarget } from '../store/gitStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { refocusGitIfLost } from './gitFocus'
import { closeDrawer, loadUntilRevealed, reloadDiff, retryFailedDetails, selectWorkingTree, takeRetry } from './gitRequests'

const COMMIT_OPERATION = 'git.commit'

const followShownFile = (state: GitState, file: GitFileTarget): void => {
  const inStaged = state.staged.some((change) => change.path === file.path)
  const inUnstaged = state.unstaged.some((change) => change.path === file.path)
  const stillThere = file.source === GitDiffSource.Staged ? inStaged : inUnstaged
  if (stillThere) {
    reloadDiff(file)
  } else if (inStaged || inUnstaged) {
    reloadDiff({ ...file, source: inStaged ? GitDiffSource.Staged : GitDiffSource.Unstaged })
  } else {
    closeDrawer()
  }
}

const finishBusy = (operation: string): void => {
  const store = useGitStore.getState()
  if (store.busy === operation) {
    store.setBusy(null)
  }
}

export const receiveGitState = (path: string, state: GitState | undefined, error: string | undefined): void => {
  const previous = useGitStore.getState().state
  useGitStore.getState().receiveState(path, state ?? null, error ?? null)
  if (state && state.conflicts.length > 0 && (previous?.conflicts.length ?? 0) === 0) {
    selectWorkingTree()
  }
  const { file, commit } = useGitStore.getState()
  if (state && file && !commit) {
    followShownFile(state, file)
  }
  retryFailedDetails()
  refocusGitIfLost()
}

export const receiveGitChanged = (path: string): void => {
  const { path: followed, file, commit } = useGitStore.getState()
  if (followed === path && file && !commit) {
    reloadDiff(file)
  }
}

export const receiveGitHistory = (history: GitHistory, error: string | undefined): void => {
  useGitStore.getState().receiveHistory(history, error ?? null)
  loadUntilRevealed()
}

export const receiveGitDiff = (request: number, diff: GitDiff | undefined, error: string | undefined): void => useGitStore.getState().receiveDiff(request, diff ?? null, error ?? null)

export const receiveGitDetails = (request: number, details: GitCommitDetails | undefined, error: string | undefined): void =>
  useGitStore.getState().receiveDetails(request, details ?? null, error ?? null)

export const receiveGitDone = (operation: string, message: string, warning: boolean): void => {
  finishBusy(operation)
  const store = useGitStore.getState()
  takeRetry(operation)
  if (operation === COMMIT_OPERATION) {
    store.setAmend(false, '')
  }
  if (operation === COMMIT_OPERATION || operation === 'git.push') {
    store.setRejection(null)
  }
  useHostStore.getState().setStatus(message, warning ? StatusLevel.Warning : StatusLevel.Info)
}

export const receiveGitFailed = (operation: string, message: string, output: string | undefined, code: GitFailureCode | undefined): void => {
  finishBusy(operation)
  const retry = takeRetry(operation)
  if (code && retry) {
    retry()
    return
  }
  useGitStore.getState().setFailure({ message, output })
  useHostStore.getState().setStatus(message, StatusLevel.Error)
}

export const receiveGitPushRejected = (operation: string, branch: string, message: string, output: string): void => {
  finishBusy(operation)
  const store = useGitStore.getState()
  if (operation === COMMIT_OPERATION) {
    store.setAmend(false, '')
  }
  store.setRejection({ branch, message, output })
  useHostStore.getState().setStatus(message, StatusLevel.Warning)
}
