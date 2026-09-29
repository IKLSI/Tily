import type { GitBranch, GitRemoteBranch, GitState, GitStash, GitTag } from '../bridge/gitMessages'
import type { ActionMenuItem } from '../components/ActionMenu'
import { useGitStore } from '../store/gitStore'
import { plural } from './gitLabels'
import { deleteBranch, deleteRemoteBranch, deleteTag, dropStash } from './gitRefActions'
import { askConfirmation, copyToClipboard, withRoot } from './gitRequests'
import { GitSelectMode, nextSelection } from './gitRows'

export enum GitRefScope {
  Local = 'local',
  Remote = 'remote',
  Tags = 'tags',
  Stashes = 'stashes',
  Worktrees = 'worktrees',
}

interface GitSelectedRefs {
  branches: GitBranch[]
  remoteBranches: GitRemoteBranch[]
  tags: GitTag[]
  stashes: GitStash[]
}

export interface GitRefMenu {
  label: string
  items: ActionMenuItem[]
}

export const refKey = (scope: GitRefScope, id: string): string => `${scope}\n${id}`

const within = <T>(items: T[], scope: GitRefScope, idOf: (item: T) => string, keys: ReadonlySet<string>): T[] => items.filter((item) => keys.has(refKey(scope, idOf(item))))

const selectedRefs = (keys: ReadonlySet<string>, state: GitState): GitSelectedRefs => ({
  branches: within(state.branches, GitRefScope.Local, (branch) => branch.name, keys),
  remoteBranches: within(state.remoteBranches, GitRefScope.Remote, (branch) => branch.name, keys),
  tags: within(state.tags, GitRefScope.Tags, (tag) => tag.name, keys),
  stashes: within(state.stashes, GitRefScope.Stashes, (stash) => stash.sha, keys),
})

const countOf = ({ branches, remoteBranches, tags, stashes }: GitSelectedRefs): number => branches.length + remoteBranches.length + tags.length + stashes.length

const deletableOf = (refs: GitSelectedRefs): GitSelectedRefs => ({ ...refs, branches: refs.branches.filter((branch) => !branch.current) })

const namesOf = ({ branches, remoteBranches, tags, stashes }: GitSelectedRefs): string[] => [
  ...branches.map((branch) => branch.name),
  ...remoteBranches.map((branch) => branch.name),
  ...tags.map((tag) => tag.name),
  ...stashes.map((stash) => `stash@{${stash.index}}`),
]

const actedKeys = (key: string): ReadonlySet<string> => {
  const { keys } = useGitStore.getState().refSelection
  return keys.has(key) ? keys : new Set([key])
}

export const applyRefSelection = (order: string[], key: string, mode: GitSelectMode): void => {
  const { refSelection, setRefSelection } = useGitStore.getState()
  const { keys, anchor } = refSelection
  const keepAnchor = mode === GitSelectMode.Range && anchor !== null && order.includes(anchor)
  setRefSelection({ keys: nextSelection(order, keys, anchor, key, mode), anchor: keepAnchor ? anchor : key })
}

export const selectAllRefs = (order: string[], focused: string | undefined): void => useGitStore.getState().setRefSelection({ keys: new Set(order), anchor: focused ?? order[0] ?? null })

export const clearRefSelection = (): void => useGitStore.getState().setRefSelection({ keys: new Set(), anchor: null })

const confirmRefsDeletion = ({ branches, remoteBranches, tags, stashes }: GitSelectedRefs): void => {
  const unmerged = branches.filter((branch) => !branch.merged).length
  const total = branches.length + remoteBranches.length + tags.length + stashes.length
  const warnings = [
    unmerged > 0 ? `${plural(unmerged, 'branche a', 'branches ont')} des commits sans merge : ils ne seront plus visibles dans le graphe.` : '',
    remoteBranches.length > 0 ? 'Les branches distantes seront supprimées sur le dépôt distant, pour tous ceux qui les utilisent, sans annulation possible depuis Dock.' : '',
  ].filter(Boolean)
  const lines = [
    ...branches.map((branch) => `Branche ${branch.name}${branch.merged ? '' : ' (sans merge)'}`),
    ...remoteBranches.map((branch) => `Branche distante ${branch.name}`),
    ...tags.map((tag) => `Tag ${tag.name}`),
    ...stashes.map((stash) => `Stash « ${stash.message} »`),
  ]
  askConfirmation({
    title: `Supprimer ${plural(total, 'référence', 'références')} ?`,
    body: warnings.length > 0 ? warnings.join(' ') : 'Les références suivantes seront supprimées.',
    detail: `${lines.join('\n')}\n\n« Annuler » dans la vue Git peut restaurer les branches, tags et stash locaux tant qu’aucune autre opération n’est faite.`,
    confirmLabel: 'Supprimer',
    run: () =>
      withRoot((path) => ({
        type: 'git.refsDelete',
        path,
        branches: branches.map((branch) => branch.name),
        remoteBranches: remoteBranches.map((branch) => branch.name),
        tags: tags.map((tag) => tag.name),
        stashes: stashes.map((stash) => stash.sha),
        confirmed: true,
      })),
  })
}

const deleteRefs = (refs: GitSelectedRefs): void => {
  const { branches, remoteBranches, tags, stashes } = refs
  if (countOf(refs) > 1) {
    confirmRefsDeletion(refs)
  } else if (branches[0]) {
    deleteBranch(branches[0])
  } else if (remoteBranches[0]) {
    deleteRemoteBranch(remoteBranches[0])
  } else if (tags[0]) {
    deleteTag(tags[0])
  } else if (stashes[0]) {
    dropStash(stashes[0])
  }
}

export const deleteSelectedRefs = (key: string): void => {
  const { state } = useGitStore.getState()
  if (state) {
    deleteRefs(deletableOf(selectedRefs(actedKeys(key), state)))
  }
}

const selectionMenu = (refs: GitSelectedRefs): ActionMenuItem[] => {
  const deletable = deletableOf(refs)
  const count = countOf(deletable)
  const names = namesOf(refs)
  const items: (ActionMenuItem | false)[] = [
    count > 0 && { id: 'delete', label: `Supprimer ${plural(count, 'référence', 'références')}…`, run: () => confirmRefsDeletion(deletable) },
    { id: 'copy', label: `Copier les ${names.length} noms`, run: () => copyToClipboard(names.join('\n'), `${names.length} noms copiés.`) },
  ]
  return items.filter((item): item is ActionMenuItem => item !== false)
}

export const refMenu = (key: string, single: GitRefMenu): GitRefMenu => {
  const { state, refSelection, setRefSelection } = useGitStore.getState()
  if (!refSelection.keys.has(key)) {
    setRefSelection({ keys: new Set([key]), anchor: key })
    return single
  }
  const refs = state ? selectedRefs(refSelection.keys, state) : null
  return refs && countOf(refs) > 1 ? { label: `Actions de ${plural(countOf(refs), 'référence', 'références')}`, items: selectionMenu(refs) } : single
}
