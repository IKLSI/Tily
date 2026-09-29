import { memo, type MouseEvent } from 'react'
import type { FileEntry } from '../bridge/messages'
import { renameSelectionEnd } from '../explorer/fileTree'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { InlineNameEditor } from './InlineNameEditor'
import { rowIndent, type FileTreeHandlers } from './fileTreeHandlers'

interface FileTreeRowProps {
  entry: FileEntry
  parent: string
  depth: number
  selected: boolean
  focusable: boolean
  expanded: boolean
  renaming: boolean
  handlers: FileTreeHandlers
}

export const FileTreeRow = memo(function FileTreeRow({ entry, parent, depth, selected, focusable, expanded, renaming, handlers }: FileTreeRowProps) {
  const handleClick = () => {
    handlers.select(entry.path)
    if (entry.isDirectory && !renaming) {
      handlers.toggle(entry.path)
    }
  }
  const handleDoubleClick = () => {
    if (!entry.isDirectory) {
      handlers.activate(entry)
    }
  }
  const handleContextMenu = (event: MouseEvent) => {
    event.preventDefault()
    event.stopPropagation()
    handlers.select(entry.path)
    handlers.openMenu({ x: event.clientX, y: event.clientY, entry, parent })
  }
  const handleCommitRename = (name: string) => handlers.commitRename(entry, parent, name)

  return (
    <div
      role="treeitem"
      data-file-row={entry.path}
      tabIndex={focusable ? 0 : -1}
      aria-level={depth + 1}
      aria-selected={selected}
      aria-expanded={entry.isDirectory ? expanded : undefined}
      className={`flex h-[22px] cursor-pointer items-center gap-[5px] pr-[8px] text-[12px] select-none ${selected ? 'bg-dock-green-soft text-dock-green-deep' : 'text-dock-ink-soft hover:bg-dock-green-hover hover:text-dock-ink'}`}
      style={{ paddingLeft: rowIndent(depth) }}
      onClick={handleClick}
      onDoubleClick={handleDoubleClick}
      onContextMenu={handleContextMenu}
    >
      <span className="flex w-[12px] shrink-0 justify-center text-dock-muted">
        {entry.isDirectory && <Icon name={IconName.Chevron} size={10} className={expanded ? 'rotate-90' : ''} />}
      </span>
      <Icon name={entry.isDirectory ? IconName.Folder : IconName.File} className="shrink-0 text-dock-muted" />
      {renaming ? (
        <InlineNameEditor value={entry.name} label={`Nouveau nom de ${entry.name}`} className="h-[18px] min-w-0 flex-1 text-[12px]" selectionEnd={renameSelectionEnd(entry)} onCommit={handleCommitRename} onCancel={handlers.cancelRename} />
      ) : (
        <span className="min-w-0 truncate">{entry.name}</span>
      )}
    </div>
  )
})
