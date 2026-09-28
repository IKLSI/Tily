import type { GitWebMessage } from '../bridge/gitMessages'
import { shortSha } from './gitLabels'

const SHA_PATTERN = /^[0-9a-f]{40}$/

const quoted = (ref: string | null): string => (ref && SHA_PATTERN.test(ref) ? shortSha(ref) : `« ${ref ?? ''} »`)

const BUSY_LABELS: Record<string, (ref: string | null) => string> = {
  'git.fetch': () => 'Fetch en cours…',
  'git.pull': () => 'Pull en cours…',
  'git.push': () => 'Push en cours…',
  'git.commit': () => 'Commit en cours…',
  'git.merge': (ref) => `Merge de ${quoted(ref)} en cours…`,
  'git.rebase': (ref) => `Rebase sur ${quoted(ref)} en cours…`,
  'git.cherryPick': () => 'Cherry-pick en cours…',
  'git.reset': () => 'Reset en cours…',
  'git.switch': (ref) => `Checkout de ${quoted(ref)} en cours…`,
  'git.branchCreate': () => 'Création de la branche…',
  'git.branchRename': (ref) => `Renommage de la branche ${quoted(ref)}…`,
  'git.branchDelete': (ref) => `Suppression de la branche ${quoted(ref)}…`,
  'git.remoteBranchDelete': (ref) => `Suppression de la branche distante ${quoted(ref)}…`,
  'git.tagCreate': () => 'Création du tag…',
  'git.tagDelete': (ref) => `Suppression du tag ${quoted(ref)}…`,
  'git.tagPush': (ref) => `Push du tag ${quoted(ref)} en cours…`,
  'git.stash': () => 'Stash en cours…',
  'git.stashApply': () => 'Application du stash…',
  'git.stashDrop': () => 'Suppression du stash…',
  'git.undo': () => 'Annulation en cours…',
  'git.continue': () => 'Fin de l’opération en cours…',
  'git.abort': () => 'Abandon de l’opération en cours…',
}

export const busyRefOf = (message: GitWebMessage): string | null => {
  switch (message.type) {
    case 'git.branchDelete':
    case 'git.branchRename':
    case 'git.tagDelete':
    case 'git.tagPush':
      return message.name
    case 'git.switch':
    case 'git.merge':
    case 'git.rebase':
    case 'git.remoteBranchDelete':
      return message.reference
    default:
      return null
  }
}

export const busyLabel = (operation: string, ref: string | null): string => BUSY_LABELS[operation]?.(ref) ?? 'Opération Git en cours…'
