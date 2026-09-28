import { refIndent } from '../git/gitBranchTree'
import { Icon } from './Icon'
import { IconName } from './iconName'

interface GitRefFolderProps {
  name: string
  path: string
  count: number
  depth: number
  expanded: boolean
  onToggle: () => void
}

export function GitRefFolder({ name, path, count, depth, expanded, onToggle }: GitRefFolderProps) {
  return (
    <button
      type="button"
      aria-expanded={expanded}
      className="flex h-[24px] w-full shrink-0 cursor-pointer items-center gap-[6px] pr-[8px] text-left text-[12px] text-dock-ink-soft select-none hover:bg-dock-green-hover hover:text-dock-ink"
      style={{ paddingLeft: refIndent(depth) }}
      data-tip={`${path}/ · ${expanded ? 'replier' : 'déplier'}`}
      onClick={onToggle}
    >
      <Icon name={IconName.Chevron} size={10} className={`shrink-0 text-dock-muted transition-transform ${expanded ? 'rotate-90' : ''}`} />
      <Icon name={IconName.Folder} className="shrink-0 text-dock-muted" />
      <span className="min-w-0 flex-1 truncate">{name}</span>
      <span className="shrink-0 text-[11px] text-dock-muted">{count}</span>
    </button>
  )
}
