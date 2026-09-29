import type { IMarker, Terminal } from '@xterm/xterm'
import { COMMAND_DONE_OSC } from './commandNotices'

const CWD_OSC = 7
const ENTER = '\r'
const EXEC_KIND = 'exec'
const DONE_KIND = 'done'
const PROMPT_KIND = 'prompt'
const SEPARATOR = ';'
const NEXT_ROW = 1
const SAME_ROW = 0

interface OutputStart {
  marker: IMarker
  offset: number
}

interface OutputRange {
  start: OutputStart
  end: IMarker | null
  promptRows: number
}

interface CommandTrack {
  announcesExecution: boolean
  prompted: boolean
  outputStart: OutputStart | null
  candidate: OutputRange | null
  settling: OutputRange | null
  last: OutputRange | null
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

const startOutput = (track: CommandTrack, terminal: Terminal, offset: number): void => {
  release(track.candidate)
  track.candidate = null
  track.outputStart?.marker.dispose()
  const marker = terminal.registerMarker(0)
  track.outputStart = marker ? { marker, offset } : null
}

export const trackCommandOutput = (terminal: Terminal): void => {
  const track: CommandTrack = { announcesExecution: false, prompted: false, outputStart: null, candidate: null, settling: null, last: null }
  tracks.set(terminal, track)
  terminal.onData((data) => {
    track.settling = null
    if (!track.announcesExecution && track.prompted && !track.outputStart && data.includes(ENTER)) {
      startOutput(track, terminal, NEXT_ROW)
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
    return false
  })
  terminal.parser.registerOscHandler(COMMAND_DONE_OSC, (data) => {
    const [kind, value] = data.split(SEPARATOR)
    const rows = Number(value)
    if (kind === EXEC_KIND) {
      track.announcesExecution = true
      track.settling = null
      startOutput(track, terminal, terminal.buffer.active.cursorX === 0 ? SAME_ROW : NEXT_ROW)
    }
    if (kind === PROMPT_KIND && track.settling && Number.isInteger(rows) && rows > 0) {
      track.settling.promptRows = rows
    }
    if (kind === DONE_KIND && track.candidate) {
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
  const firstRow = start.line + last.start.offset
  const promptStart = Math.max(firstRow, last.end.line - (last.promptRows - 1))
  const lines: string[] = []
  for (let row = firstRow; row < promptStart; row++) {
    const line = buffer.getLine(row)
    const text = line?.translateToString(true) ?? ''
    if (line?.isWrapped && lines.length > 0) {
      lines[lines.length - 1] += text
    } else {
      lines.push(text)
    }
  }
  const output = lines.join('\n').replace(/^\s*\n/, '').trimEnd()
  return output.length > 0 ? { text: output } : { failure: OutputFailure.Empty }
}
