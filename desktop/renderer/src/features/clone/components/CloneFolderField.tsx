import type { ChangeEvent } from 'react'
import type { ClonePlan } from '../../../bridge/cloneMessages'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { SETTINGS_BROWSE, SETTINGS_INPUT } from '../../settings/components/settingsStyles'
import { changeCloneDraft, pickCloneFolder } from '../cloneActions'
import type { CloneDraft } from '../cloneStore'

interface CloneFolderFieldProps {
  draft: CloneDraft
  plan: ClonePlan | null
  roots: string[]
}

const BROWSE_LABEL = 'Choisir un autre dossier de destination'

const handleFolderChange = (event: ChangeEvent<HTMLSelectElement>) => changeCloneDraft({ folder: event.target.value })

export function CloneFolderField({ draft, plan, roots }: CloneFolderFieldProps) {
  const folder = draft.folder ?? plan?.folder ?? ''
  const options = [...new Set([...roots, folder])].filter(Boolean)

  return (
    <div className="flex gap-1">
      <select className={SETTINGS_INPUT} value={folder} aria-label="Dossier de destination" onChange={handleFolderChange}>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <button type="button" className={SETTINGS_BROWSE} aria-label={BROWSE_LABEL} data-tip={BROWSE_LABEL} onClick={pickCloneFolder}>
        <Icon name={IconName.Folder} />
      </button>
    </div>
  )
}
