import { StatusLevel, useHostStore } from '../store/hostStore'
import { terminalRegistry } from './terminalRegistry'

const COPY_FAILED = 'Copie dans le presse-papiers impossible.'
const PASTE_FAILED = 'Lecture du presse-papiers impossible.'
const NO_MOUSE_TRACKING = 'none'

const reportFailure = (message: string) => (): void => useHostStore.getState().setStatus(message, StatusLevel.Error)

export const hasPaneSelection = (paneId: string): boolean => terminalRegistry.get(paneId)?.terminal.hasSelection() ?? false

export const copyPaneSelection = (paneId: string): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (terminal?.hasSelection()) {
    void navigator.clipboard.writeText(terminal.getSelection()).catch(reportFailure(COPY_FAILED))
    terminal.clearSelection()
  }
}

export const pasteIntoPane = (paneId: string): void => {
  const terminal = terminalRegistry.get(paneId)?.terminal
  if (terminal) {
    void navigator.clipboard
      .readText()
      .then((text) => terminal.paste(text))
      .catch(reportFailure(PASTE_FAILED))
  }
}

export const selectAllInPane = (paneId: string): void => terminalRegistry.get(paneId)?.terminal.selectAll()

export const isMouseTrackedByProgram = (paneId: string): boolean => (terminalRegistry.get(paneId)?.terminal.modes.mouseTrackingMode ?? NO_MOUSE_TRACKING) !== NO_MOUSE_TRACKING

export const focusPaneTerminal = (paneId: string): void => terminalRegistry.get(paneId)?.terminal.focus()
