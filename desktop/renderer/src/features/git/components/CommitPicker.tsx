import { useMemo } from 'react'
import { useShallow } from 'zustand/react/shallow'
import type { GitCommit } from '../../../bridge/gitMessages'
import { closeCommitPicker, goToCommit } from '../commitSearch'
import { shortDate } from '../gitLabels'
import type { SearchItem } from '../../palette/searchFilter'
import { useCommitPickerStore } from '../commitPickerStore'
import { useGitStore } from '../gitStore'
import { SearchDialog } from '../../palette/components/SearchDialog'

const SHORT_SHA = 7
const MAX_RESULTS = 200
const HINT_SEPARATOR = ' · '

const itemOf = (commit: GitCommit): SearchItem => ({
  id: commit.sha,
  label: commit.subject,
  hint: [commit.sha.slice(0, SHORT_SHA), commit.author, shortDate(commit.date)].join(HINT_SEPARATOR),
  searchText: [commit.sha, commit.author, commit.email, ...commit.refs.map((ref) => ref.name)].join(' '),
})

const handleRun = (item: SearchItem) => goToCommit(item.id)

export function CommitPicker() {
  const open = useCommitPickerStore((state) => state.open)
  const { history, name } = useGitStore(useShallow((state) => ({ history: state.history, name: state.state?.name ?? '' })))
  const items = useMemo(() => (open && history ? history.commits.filter((commit) => !commit.stash).map(itemOf) : []), [open, history])
  if (!open) {
    return null
  }
  const count = items.length
  const footer = `${count === 1 ? '1 commit chargé' : `${count} commits chargés`}${history?.hasMore ? ' : les plus anciens ne sont pas encore chargés dans le graphe' : ''} · Entrée : afficher le commit dans le graphe`

  return (
    <SearchDialog
      label="Rechercher un commit"
      placeholder={`Message, SHA, auteur ou branche dans ${name}…`}
      emptyMessage={count === 0 ? 'Aucun commit chargé.' : 'Aucun commit ne correspond à la recherche.'}
      items={items}
      maxResults={MAX_RESULTS}
      onClose={closeCommitPicker}
      onRun={handleRun}
      footer={footer}
    />
  )
}
