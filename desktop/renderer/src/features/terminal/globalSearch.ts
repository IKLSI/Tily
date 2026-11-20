import type { IBuffer } from '@xterm/xterm'
import { activeTab, activeWorkspace, folderName, panesOf } from '../../model/session'
import { useSessionStore } from '../../stores/sessionStore'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { type TerminalLine } from './terminalLines'
import { useTerminalSearchStore } from './terminalSearchStore'
import { joinPane } from './terminalActions'
import { terminalRegistry } from './terminalRegistry'

const REVEAL_CONTEXT_ROWS = 3
const SEPARATOR = ' · '

const logicalLineAt = (buffer: IBuffer, row: number): string => {
  let text = ''
  for (let current = row; ; current++) {
    const continues = buffer.getLine(current + 1)?.isWrapped === true
    text += buffer.getLine(current)?.translateToString(!continues) ?? ''
    if (!continues) {
      return text
    }
  }
}

const linesOf = (paneId: string, location: string): TerminalLine[] => {
  const buffer = terminalRegistry.get(paneId)?.terminal.buffer.active
  if (!buffer) {
    return []
  }
  const lines: TerminalLine[] = []
  for (let row = buffer.length - 1; row >= 0; row--) {
    if (buffer.getLine(row)?.isWrapped === false) {
      const text = logicalLineAt(buffer, row)
      if (text.trim() !== '') {
        lines.push({ id: `${paneId}:${row}`, paneId, row, text, location })
      }
    }
  }
  return lines
}

export const collectTerminalLines = (): TerminalLine[] => {
  const { session } = useSessionStore.getState()
  if (!session) {
    return []
  }
  const workspace = activeWorkspace(session)
  const activePaneId = workspace ? activeTab(workspace).active : ''
  const located = session.workspaces.flatMap((candidate) =>
    candidate.tabs.flatMap((tab) => panesOf(tab.tree).map((pane) => ({ paneId: pane.id, location: `${candidate.name} / ${tab.name}${SEPARATOR}${folderName(pane.path)}` }))),
  )
  const activeFirst = [...located.filter(({ paneId }) => paneId === activePaneId), ...located.filter(({ paneId }) => paneId !== activePaneId)]
  return activeFirst.flatMap(({ paneId, location }) => linesOf(paneId, location))
}

export const openTerminalSearch = (query?: string): void => useTerminalSearchStore.getState().show(query)

export const closeTerminalSearch = (): void => {
  useTerminalSearchStore.getState().hide()
  focusActivePane()
}

export const goToTerminalLine = ({ paneId, row, text }: TerminalLine): void => {
  useTerminalSearchStore.getState().hide()
  joinPane(paneId)
  requestAnimationFrame(() => {
    const terminal = terminalRegistry.get(paneId)?.terminal
    if (terminal) {
      terminal.scrollToLine(Math.max(0, row - REVEAL_CONTEXT_ROWS))
      terminal.select(0, row, Math.max(1, text.length))
    }
  })
}
