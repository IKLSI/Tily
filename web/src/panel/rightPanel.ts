import { focusActivePane, focusFileTree } from '../explorer/fileExplorerActions'
import { focusGitPanel, requestGraphFocus } from '../git/gitFocus'
import { RightPanelView } from '../model/session'
import { useGitStore } from '../store/gitStore'
import { useSessionStore } from '../store/sessionStore'

const PANEL_SELECTOR = '[data-right-panel], [data-git-graph]'

const focusGit = (): void => (useGitStore.getState().graphOpen ? requestGraphFocus() : focusGitPanel())

const focusView = (view: RightPanelView): void => (view === RightPanelView.Git ? focusGit() : focusFileTree())

const focusIsInPanel = (): boolean => Boolean(document.activeElement?.closest(PANEL_SELECTOR))

export const togglePanelView = (view: RightPanelView, focusPanel: boolean): void => {
  const leaving = focusIsInPanel()
  if (view === RightPanelView.Git) {
    useGitStore.getState().setGraphOpen(true)
  }
  const opened = useSessionStore.getState().togglePanelView(view)
  if (opened && focusPanel) {
    requestAnimationFrame(() => focusView(view))
  } else if (!opened && (leaving || focusPanel)) {
    focusActivePane()
  }
}

export const toggleRightPanel = (): void => {
  const leaving = focusIsInPanel()
  useGitStore.getState().setGraphOpen(true)
  if (!useSessionStore.getState().toggleExplorer() && leaving) {
    focusActivePane()
  }
}

export const showPanelView = (view: RightPanelView): void => {
  if (view === RightPanelView.Git) {
    useGitStore.getState().setGraphOpen(true)
  }
  useSessionStore.getState().setPanelView(view)
  requestAnimationFrame(() => focusView(view))
}

export const showGitGraph = (): void => {
  useGitStore.getState().setGraphOpen(true)
  requestAnimationFrame(requestGraphFocus)
}

export const hideGitGraph = (): void => {
  useGitStore.getState().setGraphOpen(false)
  focusActivePane()
}

export const toggleGitGraph = (): void => (useGitStore.getState().graphOpen ? hideGitGraph() : showGitGraph())
