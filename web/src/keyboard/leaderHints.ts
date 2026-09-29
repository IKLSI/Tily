export interface LeaderHint {
  keys: string
  label: string
}

export const LEADER_HINTS: LeaderHint[] = [
  { keys: 'T', label: 'onglet' },
  { keys: 'V', label: 'côte à côte' },
  { keys: 'H', label: 'haut / bas' },
  { keys: 'W', label: 'workspace' },
  { keys: 'F', label: 'projet' },
  { keys: 'N', label: 'worktree' },
  { keys: 'E', label: 'fichiers' },
  { keys: 'G', label: 'git' },
  { keys: 'B', label: 'workspaces' },
  { keys: 'X', label: 'fermer le pane' },
  { keys: 'M', label: 'agrandir / réduire le pane' },
  { keys: 'Z', label: 'rouvrir' },
  { keys: 'A', label: 'agent en attente' },
  { keys: 'P', label: 'palette' },
  { keys: ',', label: 'paramètres' },
  { keys: '← ↑ → ↓', label: 'pane voisin' },
  { keys: 'PgUp / PgDn', label: 'déplacer l’onglet' },
  { keys: 'Échap', label: 'annuler' },
  { keys: 'Ctrl + Espace', label: 'envoyer au terminal' },
]
