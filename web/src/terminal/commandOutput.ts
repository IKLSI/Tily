import type { IMarker, Terminal } from '@xterm/xterm'
import { COMMAND_DONE_OSC } from './commandNotices'

const CWD_OSC = 7
const ENTER = '\r'
const DONE_KIND = 'done'
const PROMPT_KIND = 'prompt'
const SEPARATOR = ';'

interface OutputRange {
  start: IMarker
  end: IMarker | null
  promptRows: number
}

interface CommandTrack {
  prompted: boolean
  outputStart: IMarker | null
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
  range?.start.dispose()
  range?.end?.dispose()
}

export const trackCommandOutput = (terminal: Terminal): void => {
  const track: CommandTrack = { prompted: false, outputStart: null, candidate: null, settling: null, last: null }
  tracks.set(terminal, track)
  terminal.onData((data) => {
    track.settling = null
    if (track.prompted && !track.outputStart && data.includes(ENTER)) {
      release(track.candidate)
      track.candidate = null
      track.outputStart = terminal.registerMarker(0) ?? null
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
    }
    track.outputStart = null
    track.prompted = true
    return false
  })
  terminal.parser.registerOscHandler(COMMAND_DONE_OSC, (data) => {
    const [kind, value] = data.split(SEPARATOR)
    const rows = Number(value)
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
  if (!last.end || last.start.isDisposed || last.end.isDisposed || last.start.line < 0 || last.end.line < 0) {
    return { failure: OutputFailure.Trimmed }
  }
  const buffer = terminal.buffer.normal
  const promptStart = Math.max(last.start.line + 1, last.end.line - (last.promptRows - 1))
  const lines: string[] = []
  for (let row = last.start.line + 1; row < promptStart; row++) {
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
