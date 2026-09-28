import { allPanes, type Pane } from '../model/session'
import { useHostStore } from '../store/hostStore'
import { usePaneStore } from '../store/paneStore'
import { useSessionStore } from '../store/sessionStore'
import { terminalRegistry } from './terminalRegistry'

const paneOf = (paneId: string): Pane | undefined => {
  const { session } = useSessionStore.getState()
  return session ? allPanes(session).find((pane) => pane.id === paneId) : undefined
}

const relaunch = (paneId: string): void => {
  const pane = paneOf(paneId)
  if (!pane) {
    return
  }
  usePaneStore.getState().clear(paneId)
  terminalRegistry.restart(pane)
  terminalRegistry.get(paneId)?.terminal.focus()
  useHostStore.getState().setStatus(`Nouveau shell « ${pane.shell} » lancé dans ${pane.path}.`)
}

export const restartPane = (paneId: string): void => relaunch(paneId)

export const restartPaneIn = (paneId: string, path: string): void => {
  useSessionStore.getState().setPanePath(paneId, path)
  relaunch(paneId)
}

export const dismissPaneState = (paneId: string): void => {
  usePaneStore.getState().dismiss(paneId)
  terminalRegistry.get(paneId)?.terminal.focus()
}

export const changePaneShell = (paneId: string, shellId: string): void => {
  useSessionStore.getState().setPaneShell(paneId, shellId)
  relaunch(paneId)
}
