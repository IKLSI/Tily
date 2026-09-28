import { useGitStore } from '../store/gitStore'
import type { ActionMenuItem } from './ActionMenu'
import { FloatingMenu } from './FloatingMenu'

const handleDismiss = () => {
  const { menu, openMenu } = useGitStore.getState()
  openMenu(null)
  menu?.restoreFocus()
}

const closingFirst = (item: ActionMenuItem): ActionMenuItem => ({
  ...item,
  run: () => {
    handleDismiss()
    item.run()
  },
})

export function GitContextMenu() {
  const menu = useGitStore((store) => store.menu)
  if (!menu) {
    return null
  }

  return <FloatingMenu x={menu.x} y={menu.y} label={menu.label} items={menu.items.map(closingFirst)} onClose={handleDismiss} />
}
