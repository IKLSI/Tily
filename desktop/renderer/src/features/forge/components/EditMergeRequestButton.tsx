import type { MouseEvent } from 'react'
import type { ForgeKind, MergeRequest } from '../../../bridge/forgeMessages'
import { canEditMergeRequest, openMergeRequestEditForm } from '../mergeRequestFormActions'
import { kindNoun } from '../mergeRequestLabels'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'

interface EditMergeRequestButtonProps {
  path: string
  item: MergeRequest
  kind: ForgeKind | undefined
  className?: string
}

export function EditMergeRequestButton({ path, item, kind, className = '' }: EditMergeRequestButtonProps) {
  if (!canEditMergeRequest(item)) {
    return null
  }

  const label = `Modifier la ${kindNoun(kind)} ${item.reference}`
  const handleClick = (event: MouseEvent) => {
    event.stopPropagation()
    openMergeRequestEditForm(path, item)
  }

  return (
    <button type="button" className={`flex shrink-0 cursor-pointer items-center rounded-tily-sm p-[2px] text-tily-muted hover:text-tily-ink ${className}`} data-tip={label} aria-label={label} onClick={handleClick}>
      <Icon name={IconName.Pencil} size={10} />
    </button>
  )
}
