interface MenuShortcutProps {
  keys: string
}

export function MenuShortcut({ keys }: MenuShortcutProps) {
  return <span className="text-[11px] text-tily-muted">{keys}</span>
}
