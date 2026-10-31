import { closeMergeRequestMenu, mergeRequestMenuItems } from '../mergeRequestMenu'
import { kindNoun } from '../mergeRequestLabels'
import { useForgeStore } from '../forgeStore'
import { FloatingMenu } from '../../../components/FloatingMenu'

export function MergeRequestContextMenu() {
  const menu = useForgeStore((store) => store.menu)
  if (!menu) {
    return null
  }

  return <FloatingMenu x={menu.x} y={menu.y} label={`Actions de la ${kindNoun(menu.kind)}`} items={mergeRequestMenuItems(menu)} onClose={closeMergeRequestMenu} />
}
