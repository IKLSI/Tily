import type { ILink, Terminal } from '@xterm/xterm'
import { bridge } from '../bridge/bridge'
import { allPanes } from '../model/session'
import { useSessionStore } from '../store/sessionStore'

const FILE_PATTERN = /(?:[A-Za-z]:[\\/]|\.{1,2}[\\/])?(?:[\w.@+-]+[\\/])*[\w@+-][\w.@+-]*\.[A-Za-z][A-Za-z0-9]{0,9}(?::(\d+)(?::(\d+))?|\((\d+)(?:,\s*(\d+))?\))?/g
const URL_BEFORE = /\S*:\/\/\S*$/
const SPACED_PATH_BEFORE = /[A-Za-z]:[\\/]\S*\s$/
const SEPARATOR = /[\\/]/
const PATH_CONTINUATION = /[\w.@+\\/:-]/

export interface FileLinkMatch {
  index: number
  text: string
  path: string
  line: number
  column: number
}

const numberOf = (value: string | undefined): number => (value ? Number(value) : 0)

export const findFileLinks = (text: string): FileLinkMatch[] =>
  [...text.matchAll(FILE_PATTERN)].flatMap((match) => {
    const index = match.index ?? 0
    const [whole, colonLine, colonColumn, parenLine, parenColumn] = match
    const line = numberOf(colonLine ?? parenLine)
    const path = whole.replace(/(?::\d+(?::\d+)?|\(\d+(?:,\s*\d+)?\))$/, '')
    const partOfWord = index > 0 && PATH_CONTINUATION.test(text[index - 1])
    const before = text.slice(0, index)
    if (partOfWord || URL_BEFORE.test(before) || SPACED_PATH_BEFORE.test(before) || (!SEPARATOR.test(path) && line === 0)) {
      return []
    }
    return [{ index, text: whole, path, line, column: numberOf(colonColumn ?? parenColumn) }]
  })

const folderOf = (paneId: string): string | undefined => {
  const { session } = useSessionStore.getState()
  return session ? allPanes(session).find((pane) => pane.id === paneId)?.path : undefined
}

export const registerFileLinks = (terminal: Terminal, paneId: string): void => {
  terminal.registerLinkProvider({
    provideLinks: (lineNumber, callback) => {
      const text = terminal.buffer.active.getLine(lineNumber - 1)?.translateToString(true) ?? ''
      const links: ILink[] = findFileLinks(text).map((match) => ({
        text: match.text,
        range: { start: { x: match.index + 1, y: lineNumber }, end: { x: match.index + match.text.length, y: lineNumber } },
        activate: (event) => {
          if (event.ctrlKey) {
            bridge.send({ type: 'files.openAt', path: match.path, cwd: folderOf(paneId), line: match.line, column: match.column })
          }
        },
      }))
      callback(links.length > 0 ? links : undefined)
    },
  })
}
