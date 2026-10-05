import type { ChangeEvent } from 'react'
import type { Settings } from '../../../bridge/messages'
import type { WorktreeProjectFolder, WorktreeSettings } from '../../../bridge/worktreeMessages'
import { InfoTip } from '../../../components/InfoTip'
import { BrowseButton } from './BrowseButton'
import { SETTINGS_INPUT, SETTINGS_LABEL, SETTINGS_ROW } from './settingsStyles'
import { WorktreeFolderSettings } from './WorktreeFolderSettings'

interface ProjectSettingsSectionProps {
  sectionClassName: string
  settings: Settings
  onProjectsRootChange: (projectsRoot: string) => void
  onPickProjectsRoot: () => void
  onWorktreesChange: (patch: Partial<WorktreeSettings>) => void
  onWorktreeFoldersChange: (folders: WorktreeProjectFolder[]) => void
  onPickWorktreeFolder: () => void
}

const PROJECTS_ROOT_ID = 'settings-projects-root'
const DEFAULT_BASE_ID = 'settings-default-base'

export function ProjectSettingsSection({ sectionClassName, settings, onProjectsRootChange, onPickProjectsRoot, onWorktreesChange, onWorktreeFoldersChange, onPickWorktreeFolder }: ProjectSettingsSectionProps) {
  const handleProjectsRootChange = (event: ChangeEvent<HTMLInputElement>) => onProjectsRootChange(event.target.value)
  const handleDefaultBaseChange = (event: ChangeEvent<HTMLInputElement>) => onWorktreesChange({ defaultBase: event.target.value })

  return (
    <section className="flex flex-col gap-2">
      <h3 className={sectionClassName}>Projets</h3>
      <div className="flex flex-col gap-1">
        <span className={SETTINGS_ROW}>
          <label htmlFor={PROJECTS_ROOT_ID} className={SETTINGS_LABEL}>
            Dossier racine des projets
          </label>
          <InfoTip text="Ses dossiers de premier niveau sont listés par le sélecteur de projets, hors « worktrees » et dossiers cachés." />
        </span>
        <span className="flex gap-1">
          <input id={PROJECTS_ROOT_ID} type="text" className={SETTINGS_INPUT} value={settings.projectsRoot} spellCheck={false} onChange={handleProjectsRootChange} />
          <BrowseButton tip="Choisir le dossier des projets" onClick={onPickProjectsRoot} />
        </span>
      </div>
      <WorktreeFolderSettings worktrees={settings.worktrees} folders={settings.worktreeFolders} projectsRoot={settings.projectsRoot} onChange={onWorktreesChange} onFoldersChange={onWorktreeFoldersChange} onPickFolder={onPickWorktreeFolder} />
      <div className="flex flex-col gap-1">
        <span className={SETTINGS_ROW}>
          <label htmlFor={DEFAULT_BASE_ID} className={SETTINGS_LABEL}>
            Base par défaut des nouvelles branches
          </label>
          <InfoTip text="Branche de départ proposée à la création d’un worktree, récupérée par un fetch sur origin juste avant." />
        </span>
        <input id={DEFAULT_BASE_ID} type="text" className={SETTINGS_INPUT} value={settings.worktrees.defaultBase} spellCheck={false} onChange={handleDefaultBaseChange} />
      </div>
    </section>
  )
}
