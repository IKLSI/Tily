import { RightPanelView } from '../../model/session'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { openPanelView } from '../right-panel/rightPanel'
import { useCommitPickerStore } from './commitPickerStore'
import { useGitStore } from './gitStore'
import { focusGitGraph } from './gitFocus'
import { revealCommit } from './gitRequests'

export const canSearchCommits = (): boolean => {
  const { path, resolved, state, history } = useGitStore.getState()
  return path !== '' && path === resolved && Boolean(state && history && history.commits.length > 0)
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
