import type { ActionMenuItem } from './ActionMenu'
import { FloatingMenu } from './FloatingMenu'

interface GitContextMenuProps {
  x: number
  y: number
  label: string
  items: ActionMenuItem[]
  onDismiss: () => void
}

export function GitContextMenu({ x, y, label, items, onDismiss }: GitContextMenuProps) {
  const closingFirst = (item: ActionMenuItem): ActionMenuItem => ({
    ...item,
    run: () => {
      onDismiss()
      item.run()
    },
  })

  return <FloatingMenu x={x} y={y} label={label} items={items.map(closingFirst)} onClose={onDismiss} />
}
