import { lazy } from 'react'

const loadGitViews = () => import('../features/git/components/gitViews')
const loadFileExplorer = () => import('../features/explorer/components/FileExplorer')

export const GitPanel = lazy(() => loadGitViews().then((module) => ({ default: module.GitPanel })))
export const GitGraphView = lazy(() => loadGitViews().then((module) => ({ default: module.GitGraphView })))
export const GitDiffDrawer = lazy(() => loadGitViews().then((module) => ({ default: module.GitDiffDrawer })))
export const GitContextMenu = lazy(() => loadGitViews().then((module) => ({ default: module.GitContextMenu })))
export const GitConfirmDialog = lazy(() => loadGitViews().then((module) => ({ default: module.GitConfirmDialog })))
export const FileExplorer = lazy(() => loadFileExplorer().then((module) => ({ default: module.FileExplorer })))

export const preloadViews = (): (() => void) => {
  const handle = requestIdleCallback(() => {
    void loadGitViews()
    void loadFileExplorer()
  })
  return () => cancelIdleCallback(handle)
}
