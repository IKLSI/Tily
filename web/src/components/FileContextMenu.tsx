import { EntryKind, type FileEntry } from '../bridge/messages'
import type { ActionMenuItem } from './ActionMenu'
import { FloatingMenu } from './FloatingMenu'
import { MenuShortcut } from './MenuShortcut'
import type { FileMenuRequest } from './fileTreeHandlers'

export interface FileMenuActions {
  open: (entry: FileEntry) => void
  openTerminal: (path: string) => void
  newEntry: (parent: string, kind: EntryKind) => void
  rename: (path: string) => void
  remove: (entry: FileEntry, parent: string) => void
  copyPath: (path: string) => void
  refresh: () => void
}

interface FileContextMenuProps {
  request: FileMenuRequest
  actions: FileMenuActions
  onDismiss: () => void
}

const itemsFor = ({ entry, parent }: FileMenuRequest, actions: FileMenuActions): ActionMenuItem[] => {
  if (!entry) {
    return [
      { id: 'new-file', label: 'Nouveau fichier', run: () => actions.newEntry(parent, EntryKind.File) },
      { id: 'new-folder', label: 'Nouveau dossier', run: () => actions.newEntry(parent, EntryKind.Folder) },
      { id: 'terminal', label: 'Ouvrir un terminal ici', run: () => actions.openTerminal(parent) },
      { id: 'copy-path', label: 'Copier le chemin', run: () => actions.copyPath(parent) },
      { id: 'refresh', label: 'Actualiser', run: actions.refresh },
    ]
  }
  const folder = entry.isDirectory ? entry.path : parent
  return [
    entry.isDirectory
      ? { id: 'terminal', label: 'Ouvrir un terminal ici', run: () => actions.openTerminal(entry.path) }
      : { id: 'open', label: 'Ouvrir dans l’éditeur', detail: <MenuShortcut keys="Entrée" />, run: () => actions.open(entry) },
    { id: 'new-file', label: 'Nouveau fichier', run: () => actions.newEntry(folder, EntryKind.File) },
    { id: 'new-folder', label: 'Nouveau dossier', run: () => actions.newEntry(folder, EntryKind.Folder) },
    { id: 'rename', label: 'Renommer', detail: <MenuShortcut keys="F2" />, run: () => actions.rename(entry.path) },
    { id: 'delete', label: 'Supprimer', detail: <MenuShortcut keys="Suppr" />, run: () => actions.remove(entry, parent) },
    { id: 'copy-path', label: 'Copier le chemin', run: () => actions.copyPath(entry.path) },
  ]
}

export function FileContextMenu({ request, actions, onDismiss }: FileContextMenuProps) {
  const closingFirst = (item: ActionMenuItem): ActionMenuItem => ({
    ...item,
    run: () => {
      onDismiss()
      item.run()
    },
  })

  return <FloatingMenu x={request.x} y={request.y} label={request.entry ? `Actions de ${request.entry.name}` : 'Actions du dossier'} items={itemsFor(request, actions).map(closingFirst)} onClose={onDismiss} />
}
