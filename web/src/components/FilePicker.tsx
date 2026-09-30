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

const itemsOf = (list: ProjectFileList | null): SearchItem[] =>
  (list?.files ?? []).map((relative) => {
    const match = LAST_SEPARATOR.exec(relative)
    return match ? { id: relative, label: match[2], hint: match[1] } : { id: relative, label: relative }
  })

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
