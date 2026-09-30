import { bridge } from '../bridge/bridge'
import type { HostMessageOf } from '../bridge/messages'
import { activePane, activeTab, activeWorkspace, folderName } from '../model/session'
import { revealInFileTree } from '../panel/rightPanel'
import { useFilePickerStore } from '../store/filePickerStore'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { focusActivePane, insertPathInActivePane } from './fileExplorerActions'

const BACKSLASH = '\\'

const fullPath = (root: string, relative: string): string => (root.endsWith(BACKSLASH) ? `${root}${relative}` : `${root}${BACKSLASH}${relative}`)

export const openFilePicker = (): void => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  if (!workspace) {
    return
  }
  const folder = activePane(activeTab(workspace)).path
  useFilePickerStore.getState().open(folder)
  bridge.send({ type: 'files.search', path: folder })
}

export const closeFilePicker = (): void => {
  useFilePickerStore.getState().close()
  focusActivePane()
}

export const openProjectFile = (root: string, relative: string): void => {
  const path = fullPath(root, relative)
  useFilePickerStore.getState().remember(root, relative)
  useFilePickerStore.getState().close()
  bridge.send({ type: 'files.open', path })
  useHostStore.getState().setStatus(`Ouverture dans l’éditeur : ${folderName(path)}`)
  focusActivePane()
}

export const insertProjectFilePath = (root: string, relative: string): void => {
  useFilePickerStore.getState().remember(root, relative)
  useFilePickerStore.getState().close()
  insertPathInActivePane(fullPath(root, relative))
  focusActivePane()
}

export const revealProjectFile = (root: string, relative: string): void => {
  useFilePickerStore.getState().remember(root, relative)
  useFilePickerStore.getState().close()
  if (!revealInFileTree(fullPath(root, relative))) {
    focusActivePane()
  }
}

export const receiveProjectFiles = (message: HostMessageOf<'files.searched'>): void =>
  useFilePickerStore.getState().receive(message.path, { root: message.root, files: message.files, changed: message.changed, truncated: message.truncated, error: message.error ?? null })
