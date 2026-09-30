import type { IBufferLine, ILink, Terminal } from '@xterm/xterm'
import { bridge } from '../bridge/bridge'
import { allPanes } from '../model/session'
import { useSessionStore } from '../store/sessionStore'

const FILE_PATTERN = /(?:[A-Za-z]:[\\/]|\.{1,2}[\\/])?(?:[\p{L}\p{N}_.@+-]+[\\/])*[\p{L}\p{N}_@+-][\p{L}\p{N}_.@+-]*\.[A-Za-z][A-Za-z0-9]{0,9}(?::(\d+)(?::(\d+))?|\((\d+)(?:,\s*(\d+))?\))?/gu
const SPACED_ABSOLUTE_PATTERN = /[A-Za-z]:[\\/](?:[\p{L}\p{N}_.@+()-]+(?: [\p{L}\p{N}_.@+()-]+)*[\\/])+[\p{L}\p{N}_@+-][\p{L}\p{N}_.@+-]*\.[A-Za-z][A-Za-z0-9]{0,9}(?::(\d+)(?::(\d+))?|\((\d+)(?:,\s*(\d+))?\))?/gu
const URL_BEFORE = /\S*:\/\/\S*$/
const EXTENSION_THEN_SPACE = /\.[A-Za-z][A-Za-z0-9]{0,9} /
const SPACED_PATH_BEFORE = /[A-Za-z]:[\\/]\S*\s$/
const SEPARATOR = /[\\/]/
const PATH_CONTINUATION = /[\p{L}\p{N}_.@+\\/:-]/u
const DOMAIN_START = /^[\p{L}\p{N}-]+(?:\.[\p{L}\p{N}-]+)+\//u
const DOMAIN_NAME = /\.(?:com|org|net|fr|io|dev|app|eu|co|uk|de)$/i
const LOCATION_SUFFIX = /(?::\d+(?::\d+)?|\(\d+(?:,\s*\d+)?\))$/
const MAX_POSITION = 2 ** 31 - 1

export interface FileLinkMatch {
  index: number
  text: string
  path: string
  line: number
  column: number
}

const positionOf = (value: string | undefined): number => {
  const number = value ? Number(value) : 0
  return number <= MAX_POSITION ? number : 0
}

const matchesOf = (text: string, pattern: RegExp, continuesPreviousLine: boolean): FileLinkMatch[] =>
  [...text.matchAll(pattern)].flatMap((match) => {
    const index = match.index ?? 0
    const [whole, colonLine, colonColumn, parenLine, parenColumn] = match
    const line = positionOf(colonLine ?? parenLine)
    const path = whole.replace(LOCATION_SUFFIX, '')
    const before = text.slice(0, index)
    const partOfWord = index > 0 ? PATH_CONTINUATION.test(text[index - 1]) : continuesPreviousLine
    const bareName = !SEPARATOR.test(path)
    if (partOfWord || URL_BEFORE.test(before) || SPACED_PATH_BEFORE.test(before) || DOMAIN_START.test(path) || (bareName && (line === 0 || DOMAIN_NAME.test(path)))) {
      return []
    }
    return [{ index, text: whole, path, line, column: positionOf(colonColumn ?? parenColumn) }]
  })

const overlaps = (match: FileLinkMatch, other: FileLinkMatch): boolean => match.index < other.index + other.text.length && other.index < match.index + match.text.length

export const findFileLinks = (text: string, continuesPreviousLine = false): FileLinkMatch[] => {
  const spaced = matchesOf(text, SPACED_ABSOLUTE_PATTERN, continuesPreviousLine).filter((match) => match.path.includes(' ') && !EXTENSION_THEN_SPACE.test(match.path))
  const plain = matchesOf(text, FILE_PATTERN, continuesPreviousLine).filter((match) => !spaced.some((other) => overlaps(match, other)))
  return [...spaced, ...plain].sort((first, second) => first.index - second.index)
}

const columnsOf = (line: IBufferLine, cols: number): number[] => {
  const columns: number[] = []
  for (let x = 0; x < cols; x++) {
    const cell = line.getCell(x)
    if (cell && cell.getWidth() > 0) {
      for (let unit = 0; unit < Math.max(1, cell.getChars().length); unit++) {
        columns.push(x)
      }
    }
  }
  return columns
}

const folderOf = (paneId: string): string | undefined => {
  const { session } = useSessionStore.getState()
  return session ? allPanes(session).find((pane) => pane.id === paneId)?.path : undefined
}

export const registerFileLinks = (terminal: Terminal, paneId: string): void => {
  terminal.registerLinkProvider({
    provideLinks: (lineNumber, callback) => {
      const line = terminal.buffer.active.getLine(lineNumber - 1)
      if (!line) {
        callback(undefined)
        return
      }
      const text = line.translateToString(true)
      const matches = findFileLinks(text, line.isWrapped)
      const columns = matches.length > 0 ? columnsOf(line, terminal.cols) : []
      const links: ILink[] = matches.map((match) => ({
        text: match.text,
        range: {
          start: { x: (columns[match.index] ?? match.index) + 1, y: lineNumber },
          end: { x: (columns[match.index + match.text.length - 1] ?? match.index + match.text.length - 1) + 1, y: lineNumber },
        },
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
