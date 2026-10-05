import type { IBuffer, Terminal } from '@xterm/xterm'

interface LogicalLine {
  row: number
  text: string
}

interface CappedText {
  text: string
  lines: number
  truncated: boolean
}

const LINE_BREAK = '\n'
const BLANK_EDGES = /^(?:[ \t]*\n)+|(?:\n[ \t]*)+$/g

export const lastContentRow = (buffer: IBuffer): number => {
  const cursorRow = buffer.baseY + buffer.cursorY
  let row = buffer.length - 1
  while (row > cursorRow && (buffer.getLine(row)?.translateToString(true) ?? '') === '') {
    row--
  }
  return row
}

export const logicalLinesBetween = (buffer: IBuffer, from: number, to: number): LogicalLine[] => {
  let start = Math.max(0, from)
  while (start > 0 && buffer.getLine(start)?.isWrapped) {
    start--
  }
  const lines: LogicalLine[] = []
  for (let row = start; row <= to; row++) {
    const line = buffer.getLine(row)
    if (!line) {
      break
    }
    const text = line.translateToString(!buffer.getLine(row + 1)?.isWrapped)
    const previous = lines.at(-1)
    if (line.isWrapped && previous) {
      previous.text += text
    } else {
      lines.push({ row, text })
    }
  }
  return lines
}

const firstRowOfLastLines = (buffer: IBuffer, end: number, count: number): number => {
  let found = 0
  for (let row = end; row > 0; row--) {
    if (!buffer.getLine(row)?.isWrapped) {
      found++
      if (found === count) {
        return row
      }
    }
  }
  return 0
}

export const capText = (text: string, maxChars: number): CappedText => {
  const trimmed = text.replace(BLANK_EDGES, '')
  if (trimmed.length <= maxChars) {
    return { text: trimmed, lines: trimmed === '' ? 0 : trimmed.split(LINE_BREAK).length, truncated: false }
  }
  const tail = trimmed.slice(-maxChars)
  const kept = tail.slice(tail.indexOf(LINE_BREAK) + 1)
  return { text: kept, lines: kept.split(LINE_BREAK).length, truncated: true }
}

export const lastLines = (text: string, count: number): string => text.split(LINE_BREAK).slice(-count).join(LINE_BREAK)

export const recentText = (terminal: Terminal, count: number): string => {
  const buffer = terminal.buffer.active
  const end = lastContentRow(buffer)
  return logicalLinesBetween(buffer, firstRowOfLastLines(buffer, end, count), end)
    .map((line) => line.text.trimEnd())
    .join(LINE_BREAK)
    .replace(BLANK_EDGES, '')
}
