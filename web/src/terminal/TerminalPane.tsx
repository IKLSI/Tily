import { useEffect, useLayoutEffect, useRef, type MouseEvent } from 'react'
import type { Pane } from '../model/session'
import { handleTerminalKey } from '../keyboard/shortcuts'
import { useUiStore } from '../store/uiStore'
import { copyPaneSelection, focusPane, hasPaneAgent, hasPaneSelection, insertAgentLineBreak, isMouseTrackedByProgram, isPaneOnAlternateScreen, pasteIntoPane, pasteTextIntoPane } from './terminalActions'
import { terminalRegistry } from './terminalRegistry'

interface TerminalPaneProps {
  pane: Pane
  active: boolean
  onFocus: (paneId: string) => void
  onContextMenu: (x: number, y: number) => void
}

const MOUSE_RIGHT_BUTTON = 2
const PANE_SELECTOR = '[data-pane-id]'
const PLAIN_TEXT = 'text/plain'

export function TerminalPane({ pane, active, onFocus, onContextMenu }: TerminalPaneProps) {
  const hostRef = useRef<HTMLDivElement>(null)
  const paneRef = useRef(pane)

  useLayoutEffect(() => {
    paneRef.current = pane
  }, [pane])

  useEffect(() => {
    const host = hostRef.current
    if (!host) {
      return
    }
    const paneId = pane.id
    const handle = terminalRegistry.attach(paneRef.current, host)
    handle.keyHandler = (event) =>
      handleTerminalKey(event, {
        hasSelection: () => hasPaneSelection(paneId),
        copySelection: () => copyPaneSelection(paneId),
        pasteClipboard: () => pasteIntoPane(paneId),
        hasAgent: () => hasPaneAgent(paneId),
        insertAgentLineBreak: () => insertAgentLineBreak(paneId),
        usesAlternateScreen: () => isPaneOnAlternateScreen(paneId),
      })
    const handleNativePaste = (event: ClipboardEvent) => {
      event.preventDefault()
      event.stopPropagation()
      pasteTextIntoPane(paneId, event.clipboardData?.getData(PLAIN_TEXT) ?? '')
    }
    host.addEventListener('paste', handleNativePaste, true)
    const observer = new ResizeObserver(() => handle.fit.fit())
    observer.observe(host)
    return () => {
      host.removeEventListener('paste', handleNativePaste, true)
      observer.disconnect()
    }
  }, [pane.id])

  useEffect(() => {
    const { renamingWorkspaceId, renamingTabId, paletteOpen, projectPickerOpen, settingsOpen, closeConfirmation } = useUiStore.getState()
    const focusAlreadyInPane = Boolean(hostRef.current?.closest(PANE_SELECTOR)?.contains(document.activeElement))
    if (active && !focusAlreadyInPane && !renamingWorkspaceId && !renamingTabId && !paletteOpen && !projectPickerOpen && !settingsOpen && !closeConfirmation) {
      focusPane(pane.id)
    }
  }, [active, pane.id])

  const handleMouseDown = () => onFocus(pane.id)

  const handleContextMenu = (event: MouseEvent<HTMLDivElement>) => {
    if (event.button === MOUSE_RIGHT_BUTTON && !event.shiftKey && isMouseTrackedByProgram(pane.id)) {
      return
    }
    event.preventDefault()
    onFocus(pane.id)
    onContextMenu(event.clientX, event.clientY)
  }

  return <div ref={hostRef} className="h-full min-h-0 p-1" onMouseDown={handleMouseDown} onContextMenu={handleContextMenu} />
}
