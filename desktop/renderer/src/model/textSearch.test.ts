import { describe, expect, it } from 'vitest'
import { DEFAULT_SEARCH_OPTIONS, firstMatch, searchPattern, SearchOption } from './textSearch'

const withOptions = (...enabled: SearchOption[]) => ({ ...DEFAULT_SEARCH_OPTIONS, ...Object.fromEntries(enabled.map((option) => [option, true])) })

describe('searchPattern', () => {
  it('ignore la casse par défaut', () => {
    const pattern = searchPattern('erreur', DEFAULT_SEARCH_OPTIONS)
    expect(pattern && firstMatch('Une ERREUR ici', pattern)).toEqual({ start: 4, length: 6 })
  })

  it('respecte la casse sur demande', () => {
    const pattern = searchPattern('erreur', withOptions(SearchOption.CaseSensitive))
    expect(pattern && firstMatch('Une ERREUR ici', pattern)).toBeNull()
  })

  it('traite les caractères spéciaux comme du texte hors mode expression', () => {
    const pattern = searchPattern('a.b', DEFAULT_SEARCH_OPTIONS)
    expect(pattern && firstMatch('axb a.b', pattern)).toEqual({ start: 4, length: 3 })
  })

  it('interprète une expression régulière en mode expression', () => {
    const pattern = searchPattern('a.b', withOptions(SearchOption.Regex))
    expect(pattern && firstMatch('axb a.b', pattern)).toEqual({ start: 0, length: 3 })
  })

  it('refuse une expression invalide', () => {
    expect(searchPattern('(', withOptions(SearchOption.Regex))).toBeNull()
  })

  it('limite au mot entier', () => {
    const pattern = searchPattern('cat', withOptions(SearchOption.WholeWord))
    expect(pattern && firstMatch('concatenate cat', pattern)).toEqual({ start: 12, length: 3 })
  })

  it('ignore une correspondance vide', () => {
    const pattern = searchPattern('x*', withOptions(SearchOption.Regex))
    expect(pattern && firstMatch('abc', pattern)).toBeNull()
  })
})
