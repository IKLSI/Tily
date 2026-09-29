import { useMemo, useState } from 'react'
import type { ShellProfile } from '../bridge/messages'
import type { Session } from '../model/session'
import { buildPaletteItems, type PaletteItem } from '../palette/paletteItems'
import { SearchDialog } from './SearchDialog'

interface CommandPaletteProps {
  session: Session
  shells: ShellProfile[]
  onClose: () => void
  onRun: (item: PaletteItem) => void
  onToggleFavorite: (commandId: string) => string | null
}

export function CommandPalette({ session, shells, onClose, onRun, onToggleFavorite }: CommandPaletteProps) {
  const items = useMemo(() => buildPaletteItems(session, shells), [session, shells])
  const [notice, setNotice] = useState<string | null>(null)
  const handleToggleFavorite = (item: PaletteItem) => setNotice(onToggleFavorite(item.id))

  return (
    <SearchDialog
      label="Commandes et navigation"
      placeholder="Commande, workspace, onglet ou pane…"
      emptyMessage="Aucun résultat."
      items={items}
      onClose={onClose}
      onRun={onRun}
      onToggleFavorite={handleToggleFavorite}
      notice={notice}
    />
  )
}
