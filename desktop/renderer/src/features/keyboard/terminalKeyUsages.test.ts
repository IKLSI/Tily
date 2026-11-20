import { describe, expect, it } from 'vitest'
import { directProblem, leaderChordProblem } from './shortcutRules'
import { terminalKeyWarning } from './terminalKeyUsages'

describe('terminalKeyWarning', () => {
  it('names the programs that lose a known chord', () => {
    expect(terminalKeyWarning('Ctrl+R')).toBe('Ctrl + R n’arrivera plus au terminal : zsh et bash (rechercher dans l’historique), Claude Code (rechercher dans l’historique), fzf (rechercher dans l’historique).')
  })

  it('warns generally for an unknown Ctrl or Alt letter', () => {
    expect(terminalKeyWarning('Ctrl+Q')).toBe('Ctrl + Q n’arrivera plus au terminal ni aux programmes des panes.')
    expect(terminalKeyWarning('Alt+Q')).toBe('Alt + Q n’arrivera plus au terminal ni aux programmes des panes.')
  })

  it('does not warn for chords the terminal never receives', () => {
    expect(terminalKeyWarning('Ctrl+Shift+Q')).toBeNull()
    expect(terminalKeyWarning('Cmd+Q')).toBeNull()
    expect(terminalKeyWarning('Ctrl+Cmd+Q')).toBeNull()
    expect(terminalKeyWarning('F5')).toBeNull()
  })
})

describe('shortcut rules', () => {
  it('accepts Ctrl plus a letter as a direct shortcut', () => {
    expect(directProblem('Ctrl+T')).toBeNull()
    expect(directProblem('Ctrl+P')).toBeNull()
  })

  it('keeps Ctrl+C reserved with its reason', () => {
    expect(directProblem('Ctrl+C')).toBe('Ctrl + C est réservé : il interrompt le programme du terminal ou copie la sélection.')
    expect(leaderChordProblem('Ctrl+C')).toBe('Ctrl + C ne peut pas servir de touche Leader : il interrompt le programme du terminal ou copie la sélection.')
  })

  it('refuses Alt+F4 as Leader with its reason', () => {
    expect(leaderChordProblem('Alt+F4')).toBe('Alt + F4 ne peut pas servir de touche Leader : il ferme Tily.')
  })
})
