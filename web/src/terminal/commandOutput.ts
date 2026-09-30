import type { IBuffer, IMarker, Terminal } from '@xterm/xterm'
import { COMMAND_DONE_OSC, decodeCommandText } from './commandNotices'

const CWD_OSC = 7
const ENTER = '\r'
const EXEC_KIND = 'exec'
const DONE_KIND = 'done'
const PROMPT_KIND = 'prompt'
const SEPARATOR = ';'
const NEXT_ROW = 1
const SAME_ROW = 0
const MAX_COMMAND_MARKS = 500
const CONTEXT_ROWS = 2
const COMMAND_TAIL_CHARS = 24
const MIN_SEARCH_TAIL_CHARS = 6
const ALTERNATE_BUFFER = 'alternate'
const ROW_SEARCH_OFFSETS = [0, -1, 1, -2, 2, -3, 3]
const LINE_BREAK = /\r?\n/

export enum CommandDirection {
  Previous = 'previous',
  Next = 'next',
}

interface OutputStart {
  marker: IMarker
  offset: number
  command: string
}

interface OutputRange {
  start: OutputStart
  end: IMarker | null
  promptRows: number
}

interface CommandTrack {
  announcesExecution: boolean
  prompted: boolean
  running: boolean
  typedStart: IMarker | null
  outputStart: OutputStart | null
  candidate: OutputRange | null
  settling: OutputRange | null
  last: OutputRange | null
  commands: OutputStart[]
}

export enum OutputFailure {
  NoCommand = 'noCommand',
  Trimmed = 'trimmed',
  Empty = 'empty',
}

const tracks = new WeakMap<Terminal, CommandTrack>()

const release = (range: OutputRange | null): void => {
  range?.start.marker.dispose()
  range?.end?.dispose()
}

const startOutput = (track: CommandTrack, marker: IMarker | undefined, offset: number, command = ''): void => {
  release(track.candidate)
  track.candidate = null
  track.outputStart?.marker.dispose()
  track.outputStart = marker ? { marker, offset, command } : null
}

const withoutSpaces = (text: string): string => text.replace(/\s+/g, '')

const firstOutputRow = (buffer: IBuffer, start: OutputStart): number => {
  const expected = start.marker.line + start.offset
  const tail = withoutSpaces(start.command.split(LINE_BREAK).at(-1) ?? '').slice(-COMMAND_TAIL_CHARS)
  if (tail.length === 0) {
    return expected
  }
  const rowText = (row: number): string => buffer.getLine(row)?.translateToString(true) ?? ''
  const holdsCommand = (row: number): boolean => {
    const before = withoutSpaces(rowText(row - 2) + rowText(row - 1))
    const end = (before + withoutSpaces(rowText(row))).lastIndexOf(tail) + tail.length
    return end > before.length && end >= tail.length
  }
  if (holdsCommand(expected - 1)) {
    return expected
  }
  const endsWithCommand = (row: number): boolean => withoutSpaces(rowText(row)).endsWith(tail)
  const commandRow = ROW_SEARCH_OFFSETS.map((delta) => expected - 1 + delta).find(tail.length < MIN_SEARCH_TAIL_CHARS ? endsWithCommand : holdsCommand)
  return commandRow === undefined ? expected : commandRow + 1
}

const isLive = (marker: IMarker): boolean => !marker.isDisposed && marker.line >= 0

const rememberCommand = (track: CommandTrack, terminal: Terminal, start: OutputStart): void => {
  if (!isLive(start.marker)) {
    return
  }
  const buffer = terminal.buffer.active
  const marker = terminal.registerMarker(start.marker.line - (buffer.baseY + buffer.cursorY))
  if (marker) {
    track.commands = [...track.commands.filter((command) => !command.marker.isDisposed), { ...start, marker }]
  }
  if (track.commands.length > MAX_COMMAND_MARKS) {
    track.commands.shift()?.marker.dispose()
  }
}

export const scrollToCommand = (terminal: Terminal, direction: CommandDirection): boolean => {
  if (terminal.buffer.active.type === ALTERNATE_BUFFER) {
    return true
  }
  const buffer = terminal.buffer.normal
  const top = buffer.viewportY
  const tops = (tracks.get(terminal)?.commands ?? [])
    .filter((command) => !command.marker.isDisposed && command.marker.line >= 0)
    .map((command) => Math.max(0, firstOutputRow(buffer, command) - 1 - CONTEXT_ROWS))
  const target = direction === CommandDirection.Previous ? tops.filter((line) => line < top).at(-1) : tops.find((line) => line > top)
  if (target === undefined) {
    if (direction === CommandDirection.Next) {
      terminal.scrollToBottom()
    }
    return false
  }
  terminal.scrollToLine(target)
  return true
}

export const trackCommandOutput = (terminal: Terminal): void => {
  const track: CommandTrack = { announcesExecution: false, prompted: false, running: false, typedStart: null, outputStart: null, candidate: null, settling: null, last: null, commands: [] }
  tracks.set(terminal, track)
  terminal.onData((data) => {
    track.settling = null
    if (!track.prompted || track.running || !data.includes(ENTER)) {
      return
    }
    track.typedStart?.dispose()
    track.typedStart = terminal.registerMarker(0) ?? null
    if (!track.announcesExecution && !track.outputStart) {
      startOutput(track, terminal.registerMarker(0), NEXT_ROW)
    }
  })
  terminal.onWriteParsed(() => {
    if (track.settling) {
      track.settling.end?.dispose()
      track.settling.end = terminal.registerMarker(0) ?? null
    }
  })
  terminal.parser.registerOscHandler(CWD_OSC, () => {
    if (track.outputStart) {
      track.candidate = { start: track.outputStart, end: null, promptRows: 1 }
      track.settling = track.candidate
      track.outputStart = null
    }
    track.prompted = true
    track.running = false
    return false
  })
  terminal.parser.registerOscHandler(COMMAND_DONE_OSC, (data) => {
    const [kind, value] = data.split(SEPARATOR)
    const rows = Number(value)
    if (kind === EXEC_KIND) {
      track.announcesExecution = true
      track.running = true
      track.settling = null
      const command = decodeCommandText(value)
      const typed = track.typedStart
      track.typedStart = null
      if (typed && isLive(typed)) {
        startOutput(track, typed, NEXT_ROW, command)
      } else {
        typed?.dispose()
        startOutput(track, terminal.registerMarker(0), terminal.buffer.active.cursorX === 0 ? SAME_ROW : NEXT_ROW, command)
      }
    }
    if (kind === PROMPT_KIND && track.settling && Number.isInteger(rows) && rows > 0) {
      track.settling.promptRows = rows
    }
    if (kind === DONE_KIND && track.candidate) {
      rememberCommand(track, terminal, track.candidate.start)
      release(track.last)
      track.last = track.candidate
      track.candidate = null
    }
    return false
  })
}

export type CommandOutput = { text: string } | { failure: OutputFailure }

export const lastCommandOutput = (terminal: Terminal): CommandOutput => {
  const last = tracks.get(terminal)?.last
  if (!last) {
    return { failure: OutputFailure.NoCommand }
  }
  const start = last.start.marker
  if (!last.end || start.isDisposed || last.end.isDisposed || start.line < 0 || last.end.line < 0) {
    return { failure: OutputFailure.Trimmed }
  }
  const buffer = terminal.buffer.normal
  const firstRow = firstOutputRow(buffer, last.start)
  const promptStart = Math.max(firstRow, last.end.line - (last.promptRows - 1))
  const lines: string[] = []
  for (let row = firstRow; row < promptStart; row++) {
    const line = buffer.getLine(row)
    const text = (line?.translateToString(true) ?? '').trimEnd()
    if (line?.isWrapped && lines.length > 0) {
      lines[lines.length - 1] += text
    } else {
      lines.push(text)
    }
  }
  const output = lines.join('\n').replace(/^\s*\n/, '').trimEnd()
  return output.length > 0 ? { text: output } : { failure: OutputFailure.Empty }
}
