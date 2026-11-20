import { describe, expect, it } from 'vitest'
import { DEFAULT_SEARCH_OPTIONS } from '../../model/textSearch'
import { findTerminalLines, matchPartsOf, type TerminalLine } from './terminalLines'

const line = (paneId: string, row: number, text: string): TerminalLine => ({ id: `${paneId}:${row}`, paneId, row, text, location: 'ws / tab' })

const LINES = [line('a', 3, 'build failed: Erreur 12'), line('a', 2, 'ok'), line('b', 7, 'une erreur de plus')]

describe('findTerminalLines', () => {
  it('compte les lignes et les panes correspondants', () => {
    const results = findTerminalLines(LINES, 'erreur', DEFAULT_SEARCH_OPTIONS, 10)
    expect(results.total).toBe(2)
    expect(results.panes).toBe(2)
    expect(results.matches.map(({ id }) => id)).toEqual(['a:3', 'b:7'])
  })

  it('limite la liste sans fausser le total', () => {
    const results = findTerminalLines(LINES, 'erreur', DEFAULT_SEARCH_OPTIONS, 1)
    expect(results.matches).toHaveLength(1)
    expect(results.total).toBe(2)
  })

  it('ne renvoie rien pour une requête vide', () => {
    expect(findTerminalLines(LINES, '', DEFAULT_SEARCH_OPTIONS, 10).total).toBe(0)
  })
})

describe('matchPartsOf', () => {
  it('découpe la ligne autour de la correspondance', () => {
    const first = findTerminalLines(LINES, 'Erreur', DEFAULT_SEARCH_OPTIONS, 10).matches[0]!
    expect(matchPartsOf(first)).toEqual({ before: 'build failed: ', matched: 'Erreur', after: ' 12' })
  })

  it('tronque le contexte trop long avant la correspondance', () => {
    const first = findTerminalLines([line('a', 0, `${'x'.repeat(100)} cible`)], 'cible', DEFAULT_SEARCH_OPTIONS, 10).matches[0]!
    expect(matchPartsOf(first).before.startsWith('…')).toBe(true)
  })
})
