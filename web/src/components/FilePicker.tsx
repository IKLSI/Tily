import { useMemo } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { closeFilePicker, insertProjectFilePath, openProjectFile, revealProjectFile } from '../explorer/projectFileActions'
import type { SearchItem } from '../palette/searchFilter'
import { useFilePickerStore, type ProjectFileList } from '../store/filePickerStore'
import { SearchDialog } from './SearchDialog'

const MAX_RESULTS = 200
const LAST_SEPARATOR = /^(.*)\\([^\\]+)$/
const LOADING = 'Chargement des fichiers…'
const NO_MATCH = 'Aucun fichier ne correspond à la recherche.'
const TRUNCATED = 'Trop de fichiers : la liste proposée est incomplète, précisez la recherche ou ouvrez le terminal dans un sous-dossier.'
const FOOTER = 'Entrée : ouvrir dans l’éditeur · Maj + Entrée : afficher dans l’arbre · Ctrl + Entrée : insérer le chemin dans le terminal'

const CHANGED_HINT = 'modifié'
const HINT_SEPARATOR = ' · '

const itemOf = (relative: string, changed: boolean): SearchItem => {
  const match = LAST_SEPARATOR.exec(relative)
  const folder = match?.[1]
  const hint = changed ? (folder ? `${CHANGED_HINT}${HINT_SEPARATOR}${folder}` : CHANGED_HINT) : folder
  return { id: relative, label: match ? match[2] : relative, hint }
}

const itemsOf = (list: ProjectFileList | null): SearchItem[] => {
  if (!list) {
    return []
  }
  const changed = new Set(list.changed)
  return [...list.changed.map((relative) => itemOf(relative, true)), ...list.files.filter((relative) => !changed.has(relative)).map((relative) => itemOf(relative, false))]
}

const emptyMessageOf = (list: ProjectFileList | null): string => {
  if (!list) {
    return LOADING
  }
  if (list.error) {
    return list.error
  }
  return list.files.length === 0 ? `Aucun fichier dans ${list.root}.` : NO_MATCH
}

export function FilePicker() {
  const { folder, list } = useFilePickerStore(useShallow((state) => ({ folder: state.folder, list: state.list })))
  const items = useMemo(() => itemsOf(list), [list])
  if (folder === null) {
    return null
  }
  const root = list?.root ?? folder
  const handleRun = (item: SearchItem) => openProjectFile(root, item.id)
  const handleRunAlternate = (item: SearchItem) => revealProjectFile(root, item.id)
  const handleRunControl = (item: SearchItem) => insertProjectFilePath(root, item.id)

  return (
    <SearchDialog
      label="Ouvrir un fichier du projet"
      placeholder={`Fichier dans ${root}…`}
      emptyMessage={emptyMessageOf(list)}
      items={items}
      maxResults={MAX_RESULTS}
      onClose={closeFilePicker}
      onRun={handleRun}
      onRunAlternate={handleRunAlternate}
      onRunControl={handleRunControl}
      footer={FOOTER}
      notice={list?.truncated ? TRUNCATED : null}
    />
  )
}
