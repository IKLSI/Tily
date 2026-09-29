import type { ActionMenuItem } from './ActionMenu'
import { FloatingMenu } from './FloatingMenu'
import { MenuShortcut } from './MenuShortcut'

export interface TerminalMenuRequest {
  x: number
  y: number
}

export interface TerminalMenuActions {
  copy: () => void
  paste: () => void
  selectAll: () => void
  splitSideBySide: () => void
  splitTopBottom: () => void
  toggleZoom: () => void
  close: () => void
}

interface TerminalContextMenuProps {
  request: TerminalMenuRequest
  canCopy: boolean
  zoomed: boolean
  actions: TerminalMenuActions
  onDismiss: () => void
}

const itemsFor = (canCopy: boolean, zoomed: boolean, actions: TerminalMenuActions): ActionMenuItem[] => [
  { id: 'copy', label: 'Copier', detail: <MenuShortcut keys="Ctrl + Maj + C" />, disabled: !canCopy, run: actions.copy },
  { id: 'paste', label: 'Coller', detail: <MenuShortcut keys="Ctrl + Maj + V" />, run: actions.paste },
  { id: 'select-all', label: 'Tout sélectionner', run: actions.selectAll },
  { id: 'split-x', label: 'Split côte à côte', detail: <MenuShortcut keys="Ctrl + Maj + D" />, run: actions.splitSideBySide },
  { id: 'split-y', label: 'Split haut / bas', detail: <MenuShortcut keys="Ctrl + Maj + H" />, run: actions.splitTopBottom },
  { id: 'zoom', label: zoomed ? 'Réduire le pane' : 'Agrandir le pane', detail: <MenuShortcut keys="Ctrl + Maj + M" />, run: actions.toggleZoom },
  { id: 'close', label: 'Fermer le pane', detail: <MenuShortcut keys="Ctrl + Maj + X" />, run: actions.close },
]

export function TerminalContextMenu({ request, canCopy, zoomed, actions, onDismiss }: TerminalContextMenuProps) {
  const closingFirst = (item: ActionMenuItem): ActionMenuItem => ({
    ...item,
    run: () => {
      onDismiss()
      item.run()
    },
  })

  return <FloatingMenu x={request.x} y={request.y} label="Actions du terminal" items={itemsFor(canCopy, zoomed, actions).map(closingFirst)} onClose={onDismiss} />
}
