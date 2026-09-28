import { bridge } from '../bridge/bridge'
import type { EntryKind, FileEntry } from '../bridge/messages'
import { activeTab, activeWorkspace, folderName } from '../model/session'
import { useExplorerStore, type DeleteRequest } from '../store/explorerStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { terminalRegistry } from '../terminal/terminalRegistry'

const TREE_SELECTOR = '[data-file-tree]'
const ROW_SELECTOR = '[data-file-row]'
const WATCH_SEPARATOR = '\n'

let watchedKey = ''

export const watchFolders = (paths: string[]): void => {
  const key = paths.join(WATCH_SEPARATOR)
  if (key !== watchedKey) {
    watchedKey = key
    bridge.send({ type: 'files.watch', paths })
  }
}

export const refreshFolders = (): void => bridge.send({ type: 'files.refresh' })

export const focusActivePane = (): void => {
  const { session } = useSessionStore.getState()
  const workspace = session ? activeWorkspace(session) : undefined
  if (workspace) {
    terminalRegistry.get(activeTab(workspace).active)?.terminal.focus()
  }
}

export const focusFileRow = (path: string | null): boolean => {
  const tree = document.querySelector<HTMLElement>(TREE_SELECTOR)
  const row = Array.from(tree?.querySelectorAll<HTMLElement>(ROW_SELECTOR) ?? []).find((candidate) => candidate.dataset.fileRow === path)
  row?.focus()
  return Boolean(row)
}

export const focusFileTree = (): void => {
  const tree = document.querySelector<HTMLElement>(TREE_SELECTOR)
  if (!focusFileRow(useExplorerStore.getState().selectedPath)) {
    const fallback = tree?.querySelector<HTMLElement>(`${ROW_SELECTOR}[tabindex="0"]`) ?? tree
    fallback?.focus()
  }
}

export const refocusFileTreeIfLost = (): void => {
  requestAnimationFrame(() => {
    if (document.activeElement === document.body) {
      focusFileTree()
    }
  })
}

const focusRowIfIdle = (path: string): void => {
  requestAnimationFrame(() => {
    const active = document.activeElement
    if (active === document.body || active?.closest(TREE_SELECTOR)) {
      focusFileRow(path)
    }
  })
}

export const openFile = (entry: FileEntry): void => {
  bridge.send({ type: 'files.open', path: entry.path })
  useHostStore.getState().setStatus(`Ouverture dans l’éditeur : ${entry.name}`)
}

export const createEntry = (parent: string, name: string, kind: EntryKind): void => {
  const trimmed = name.trim()
  if (trimmed.length > 0) {
    bridge.send({ type: 'files.create', path: parent, name: trimmed, kind })
  }
}

export const renameEntry = (entry: FileEntry, parent: string, name: string): void => {
  const trimmed = name.trim()
  if (trimmed.length > 0 && trimmed !== entry.name) {
    bridge.send({ type: 'files.rename', path: entry.path, parent, name: trimmed })
  }
}

export const confirmDelete = (request: DeleteRequest): void => {
  useExplorerStore.getState().cancelDelete()
  bridge.send({ type: 'files.delete', path: request.path, parent: request.parent })
}

export const copyEntryPath = (path: string): void => {
  void navigator.clipboard
    .writeText(path)
    .then(() => useHostStore.getState().setStatus(`Chemin copié : ${path}`))
    .catch(() => useHostStore.getState().setStatus('Copie dans le presse-papiers impossible.', StatusLevel.Error))
}

export const receiveListing = (path: string, entries: FileEntry[], total: number, error: string | undefined): void =>
  useExplorerStore.getState().setListing(path, { entries, total, error: error ?? null })

export const receiveCreated = (path: string): void => {
  useExplorerStore.getState().select(path)
  useHostStore.getState().setStatus(`Créé : ${folderName(path)}`)
  focusRowIfIdle(path)
}

export const receiveRenamed = (path: string, target: string): void => {
  useExplorerStore.getState().moved(path, target)
  useHostStore.getState().setStatus(`Renommé : ${folderName(path)} → ${folderName(target)}`)
  focusRowIfIdle(target)
}

export const receiveDeleted = (path: string): void => {
  useExplorerStore.getState().removed(path)
  useHostStore.getState().setStatus(`« ${folderName(path)} » placé dans la corbeille.`)
  refocusFileTreeIfLost()
}
