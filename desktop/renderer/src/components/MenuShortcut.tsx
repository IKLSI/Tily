import type { Command } from '../features/keyboard/commands'
import { useShortcutText } from '../features/keyboard/shortcutLabels'

interface MenuShortcutProps {
  keys: string
}

export function MenuShortcut({ keys }: MenuShortcutProps) {
  return <span className="text-[11px] text-tily-muted">{keys}</span>
}

interface CommandShortcutProps {
  command: Command
}

export function CommandShortcut({ command }: CommandShortcutProps) {
  const keys = useShortcutText(command)
  return keys ? <MenuShortcut keys={keys} /> : null
}
