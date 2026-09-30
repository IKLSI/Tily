import { RightPanelView } from '../model/session'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { openPanelView } from '../panel/rightPanel'
import { useCommitPickerStore } from '../store/commitPickerStore'
import { useGitStore } from '../store/gitStore'
import { focusGitGraph } from './gitFocus'
import { revealCommit } from './gitRequests'

export const canSearchCommits = (): boolean => {
  const { state, history } = useGitStore.getState()
  return Boolean(state && history && history.commits.length > 0)
}

export const openCommitPicker = (): void => useCommitPickerStore.getState().setOpen(true)

export const closeCommitPicker = (): void => {
  useCommitPickerStore.getState().setOpen(false)
  if (!focusGitGraph()) {
    focusActivePane()
  }
}

export const goToCommit = (sha: string): void => {
  useCommitPickerStore.getState().setOpen(false)
  openPanelView(RightPanelView.Git)
  revealCommit(sha)
}
