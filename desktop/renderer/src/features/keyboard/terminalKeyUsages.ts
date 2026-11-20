import { chordKey, formatChord, hasAlt, hasCmd, hasCtrl, hasShift, isLetterKey } from './keyChords'

enum TerminalProgram {
  Any = 'tous les programmes',
  Shell = 'zsh et bash',
  ClaudeCode = 'Claude Code',
  Fzf = 'fzf',
}

type TerminalKeyUsage = Partial<Record<TerminalProgram, string>>

const TERMINAL_KEY_USAGES: Record<string, TerminalKeyUsage> = {
  'Ctrl+A': { [TerminalProgram.Shell]: 'début de ligne', [TerminalProgram.ClaudeCode]: 'début de ligne' },
  'Ctrl+B': { [TerminalProgram.Shell]: 'caractère précédent', [TerminalProgram.ClaudeCode]: 'passer la tâche en arrière-plan' },
  'Ctrl+D': { [TerminalProgram.Shell]: 'quitter le shell', [TerminalProgram.ClaudeCode]: 'quitter' },
  'Ctrl+E': { [TerminalProgram.Shell]: 'fin de ligne', [TerminalProgram.ClaudeCode]: 'fin de ligne' },
  'Ctrl+F': { [TerminalProgram.Shell]: 'caractère suivant' },
  'Ctrl+G': { [TerminalProgram.ClaudeCode]: 'ouvrir la saisie dans l’éditeur' },
  'Ctrl+H': { [TerminalProgram.Any]: 'équivaut à Retour arrière' },
  'Ctrl+I': { [TerminalProgram.Any]: 'équivaut à Tab' },
  'Ctrl+J': { [TerminalProgram.Shell]: 'valider la commande', [TerminalProgram.ClaudeCode]: 'nouvelle ligne sans envoyer' },
  'Ctrl+K': { [TerminalProgram.Shell]: 'effacer jusqu’à la fin de ligne', [TerminalProgram.ClaudeCode]: 'effacer jusqu’à la fin de ligne' },
  'Ctrl+L': { [TerminalProgram.Shell]: 'effacer l’écran', [TerminalProgram.ClaudeCode]: 'redessiner l’écran' },
  'Ctrl+M': { [TerminalProgram.Any]: 'équivaut à Entrée' },
  'Ctrl+N': { [TerminalProgram.Shell]: 'commande suivante', [TerminalProgram.ClaudeCode]: 'élément suivant de l’historique' },
  'Ctrl+O': { [TerminalProgram.ClaudeCode]: 'afficher la transcription' },
  'Ctrl+P': { [TerminalProgram.Shell]: 'commande précédente', [TerminalProgram.ClaudeCode]: 'élément précédent de l’historique' },
  'Ctrl+R': {
    [TerminalProgram.Shell]: 'rechercher dans l’historique',
    [TerminalProgram.ClaudeCode]: 'rechercher dans l’historique',
    [TerminalProgram.Fzf]: 'rechercher dans l’historique',
  },
  'Ctrl+S': { [TerminalProgram.Shell]: 'rechercher en avant dans l’historique', [TerminalProgram.ClaudeCode]: 'mettre la saisie de côté' },
  'Ctrl+T': { [TerminalProgram.Shell]: 'inverser deux caractères', [TerminalProgram.ClaudeCode]: 'afficher la liste des tâches', [TerminalProgram.Fzf]: 'rechercher un fichier' },
  'Ctrl+U': { [TerminalProgram.Shell]: 'effacer jusqu’au début de ligne', [TerminalProgram.ClaudeCode]: 'effacer jusqu’au début de ligne' },
  'Ctrl+V': { [TerminalProgram.Shell]: 'insérer le caractère suivant tel quel', [TerminalProgram.ClaudeCode]: 'coller une image' },
  'Ctrl+W': { [TerminalProgram.Shell]: 'effacer le mot précédent', [TerminalProgram.ClaudeCode]: 'effacer le mot précédent' },
  'Ctrl+X': { [TerminalProgram.Shell]: 'préfixe de raccourcis', [TerminalProgram.ClaudeCode]: 'préfixe de raccourcis' },
  'Ctrl+Y': { [TerminalProgram.Shell]: 'coller le texte effacé', [TerminalProgram.ClaudeCode]: 'coller le texte effacé' },
  'Ctrl+Z': { [TerminalProgram.Shell]: 'suspendre le programme', [TerminalProgram.ClaudeCode]: 'suspendre Claude Code' },
  'Ctrl+Backspace': { [TerminalProgram.ClaudeCode]: 'effacer le mot précédent' },
  'Ctrl+Home': { [TerminalProgram.ClaudeCode]: 'aller au début de la conversation' },
  'Ctrl+End': { [TerminalProgram.ClaudeCode]: 'aller au dernier message' },
  'Shift+Tab': { [TerminalProgram.Shell]: 'complétion précédente', [TerminalProgram.ClaudeCode]: 'changer de mode de permission' },
  'Alt+B': { [TerminalProgram.Shell]: 'mot précédent', [TerminalProgram.ClaudeCode]: 'mot précédent' },
  'Alt+C': { [TerminalProgram.Shell]: 'mettre une majuscule au mot', [TerminalProgram.Fzf]: 'aller dans un dossier' },
  'Alt+D': { [TerminalProgram.Shell]: 'effacer le mot suivant', [TerminalProgram.ClaudeCode]: 'effacer le mot suivant' },
  'Alt+F': { [TerminalProgram.Shell]: 'mot suivant', [TerminalProgram.ClaudeCode]: 'mot suivant' },
  'Alt+L': { [TerminalProgram.Shell]: 'mettre le mot en minuscules' },
  'Alt+M': { [TerminalProgram.ClaudeCode]: 'changer de mode de permission' },
  'Alt+O': { [TerminalProgram.ClaudeCode]: 'activer le mode rapide' },
  'Alt+P': { [TerminalProgram.ClaudeCode]: 'changer de modèle' },
  'Alt+T': { [TerminalProgram.Shell]: 'inverser deux mots', [TerminalProgram.ClaudeCode]: 'activer la réflexion étendue' },
  'Alt+U': { [TerminalProgram.Shell]: 'mettre le mot en majuscules' },
  'Alt+V': { [TerminalProgram.ClaudeCode]: 'coller une image' },
  'Alt+Y': { [TerminalProgram.Shell]: 'parcourir les textes effacés', [TerminalProgram.ClaudeCode]: 'parcourir les textes effacés' },
  'Alt+.': { [TerminalProgram.Shell]: 'reprendre le dernier argument' },
}

const sendsToTerminal = (chord: string): boolean => hasCtrl(chord) !== hasAlt(chord) && !hasShift(chord) && !hasCmd(chord) && isLetterKey(chordKey(chord))

const describeUsage = (usage: TerminalKeyUsage): string =>
  Object.entries(usage)
    .map(([program, action]) => `${program} (${action})`)
    .join(', ')

export const terminalKeyWarning = (chord: string): string | null => {
  const usage = TERMINAL_KEY_USAGES[chord]
  if (usage) {
    return `${formatChord(chord)} n’arrivera plus au terminal : ${describeUsage(usage)}.`
  }
  return sendsToTerminal(chord) ? `${formatChord(chord)} n’arrivera plus au terminal ni aux programmes des panes.` : null
}
