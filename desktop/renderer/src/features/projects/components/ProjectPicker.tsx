import { useMemo } from 'react'
import type { Project } from '../../../bridge/messages'
import type { SearchItem } from '../../palette/searchFilter'
import { SearchDialog } from '../../palette/components/SearchDialog'
import { Command } from '../../keyboard/commands'
import { useShortcutText, withShortcut } from '../../keyboard/shortcutLabels'

interface ProjectPickerProps {
  projects: Project[]
  root: string
  error: string | null
  onClose: () => void
  onSelect: (project: Project, inActiveWorkspace: boolean) => void
  onClone: () => void
}

interface ProjectItem extends SearchItem {
  project?: Project
}

const WORKTREE_HINT = 'worktree · '
export const PROJECTS_LOADING = 'Chargement des projets…'

const CLONE_ITEM: ProjectItem = { id: 'cloner', label: 'Cloner un dépôt…', hint: 'git clone depuis une adresse' }
const LAST_ITEMS = [CLONE_ITEM]

const hintOf = (project: Project): string => (project.worktree ? `${WORKTREE_HINT}${project.path}` : project.path)

export function ProjectPicker({ projects, root, error, onClose, onSelect, onClone }: ProjectPickerProps) {
  const settingsText = withShortcut('Paramètres', useShortcutText(Command.Settings))
  const items = useMemo<ProjectItem[]>(() => projects.map((project) => ({ id: project.path, label: project.name, hint: hintOf(project), project })), [projects])
  const run = (item: ProjectItem, inActiveWorkspace: boolean) => {
    if (item.project) {
      onSelect(item.project, inActiveWorkspace)
    } else {
      onClone()
    }
  }
  const handleRun = (item: ProjectItem) => run(item, false)
  const handleRunInActiveWorkspace = (item: ProjectItem) => run(item, true)

  return (
    <SearchDialog
      label="Ouvrir un projet"
      placeholder={root ? `Dossier dans ${root}…` : 'Dossier de projet…'}
      emptyMessage={error ?? (root === '' ? PROJECTS_LOADING : projects.length === 0 ? `Aucun projet dans ${root} : le dossier des projets se change dans ${settingsText}.` : 'Aucun dossier ne correspond à la recherche.')}
      items={items}
      lastItems={LAST_ITEMS}
      onClose={onClose}
      onRun={handleRun}
      onRunAlternate={handleRunInActiveWorkspace}
      footer="Entrée : nouveau workspace · Maj+Entrée : nouvel onglet dans le workspace actif"
    />
  )
}
