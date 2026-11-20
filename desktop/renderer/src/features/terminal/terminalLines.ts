import { firstMatch, searchPattern, type SearchOptions, type TextMatch } from '../../model/textSearch'

export interface TerminalLine {
  id: string
  paneId: string
  row: number
  text: string
  location: string
}

export interface TerminalLineMatch extends TerminalLine {
  match: TextMatch
}

export interface TerminalLineResults {
  matches: TerminalLineMatch[]
  total: number
  panes: number
}

export interface MatchParts {
  before: string
  matched: string
  after: string
}

const CONTEXT_BEFORE_MATCH = 40
const ELLIPSIS = '…'

export const findTerminalLines = (lines: TerminalLine[], query: string, options: SearchOptions, limit: number): TerminalLineResults => {
  const results: TerminalLineResults = { matches: [], total: 0, panes: 0 }
  const pattern = query === '' ? null : searchPattern(query, options)
  if (!pattern) {
    return results
  }
  const panes = new Set<string>()
  for (const line of lines) {
    const match = firstMatch(line.text, pattern)
    if (match) {
      results.total++
      panes.add(line.paneId)
      if (results.matches.length < limit) {
        results.matches.push({ ...line, match })
      }
    }
  }
  results.panes = panes.size
  return results
}

export const matchPartsOf = ({ text, match: { start, length } }: TerminalLineMatch): MatchParts => {
  const from = Math.max(0, start - CONTEXT_BEFORE_MATCH)
  return {
    before: `${from > 0 ? ELLIPSIS : ''}${text.slice(from, start).trimStart()}`,
    matched: text.slice(start, start + length),
    after: text.slice(start + length).trimEnd(),
  }
}
