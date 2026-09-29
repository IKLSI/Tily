import { waitingPanes } from '../agents/agentSummary'
import { bridge } from '../bridge/bridge'
import type { ShellProfile } from '../bridge/messages'
import { Command, runCommand } from '../keyboard/shortcuts'
import { activeTab, activeWorkspace, FAVORITES_MAX, folderName, panesOf, type Session } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { RenameOrigin, useUiStore } from '../store/uiStore'
import { closeOtherTabsKeepingText, closeTabKeepingText, duplicateTabKeepingLayout, restoreClosedTab, restoreClosedTabAt } from '../terminal/tabLifecycle'
import { joinPane } from '../terminal/terminalActions'
import { OpenTarget } from '../bridge/messages'
import { copyPaneBranch, copyPanePath, openPaneFolder } from '../terminal/contextActions'
import type { SearchItem } from './searchFilter'

enum PaletteKind {
  Command = 'command',
  Attention = 'attention',
  Workspace = 'workspace',
  Tab = 'tab',
  Pane = 'pane',
}

export interface PaletteItem extends SearchItem {
  kind: PaletteKind
  run: () => void
}

const SEPARATOR = ' · '
const NO_STATUS = ''
const FAVORITES_FULL_STATUS = `Pas plus de ${FAVORITES_MAX} favoris : retirez une étoile avant d’en ajouter une.`

const command = (id: string, label: string, run: () => void, hint?: string): PaletteItem => ({ id, kind: PaletteKind.Command, label, hint, favorite: false, run })

const commandItems = (session: Session, shells: ShellProfile[]): PaletteItem[] => {
  const store = useSessionStore.getState()
  const ui = useUiStore.getState()
  const workspace = activeWorkspace(session)
  const tab = workspace ? activeTab(workspace) : undefined
  const items: PaletteItem[] = [
    command('new-tab', 'Nouvel onglet', () => runCommand(Command.NewTab), 'Ctrl + Maj + T'),
    ...shells.map((shell) => command(`new-tab-${shell.id}`, `Nouvel onglet${SEPARATOR}${shell.name}`, () => store.newTab(shell.id))),
    command('split-x', 'Split côte à côte', () => runCommand(Command.SplitSideBySide), 'Ctrl + Maj + D'),
    command('split-y', 'Split haut / bas', () => runCommand(Command.SplitTopBottom), 'Ctrl + Maj + H'),
    command('close-pane', 'Fermer le pane actif', () => runCommand(Command.ClosePane), 'Ctrl + Maj + X'),
    command('toggle-zoom', 'Agrandir / réduire le pane actif', () => runCommand(Command.TogglePaneZoom), 'Ctrl + Maj + M'),
    command('next-tab', 'Onglet suivant', () => runCommand(Command.NextTab), 'Ctrl + Tab'),
    command('previous-tab', 'Onglet précédent', () => runCommand(Command.PreviousTab), 'Ctrl + Maj + Tab'),
    command('new-workspace', 'Nouveau workspace', () => runCommand(Command.NewWorkspace), 'Ctrl + Maj + W'),
    command('projects', 'Ouvrir un projet', () => runCommand(Command.Projects), 'Leader puis F'),
    command('toggle-explorer', 'Afficher / masquer les fichiers', () => runCommand(Command.ToggleExplorer), 'Ctrl + Maj + E'),
    command('toggle-git', 'Afficher / masquer Git', () => runCommand(Command.ToggleGit), 'Ctrl + Maj + G'),
    command('settings', 'Paramètres', () => runCommand(Command.Settings), 'Leader puis ,'),
    command('settings-export', 'Exporter les préférences…', () => {
      runCommand(Command.Settings)
      bridge.send({ type: 'settings.export' })
    }),
    command('settings-import', 'Importer les préférences…', () => {
      runCommand(Command.Settings)
      bridge.send({ type: 'settings.import' })
    }),
    command('restore-tab', 'Rouvrir le dernier onglet fermé', restoreClosedTab, 'Ctrl + Maj + Z'),
    command('toggle-sidebar', session.sidebarCollapsed ? 'Afficher les workspaces' : 'Masquer les workspaces', () => runCommand(Command.ToggleSidebar), 'Ctrl + Maj + B'),
  ]
  if (workspace) {
    items.push(
      command('rename-workspace', 'Renommer le workspace', () => ui.startRenamingWorkspace(workspace.id, RenameOrigin.Header)),
      command('move-workspace-up', 'Monter le workspace', () => store.moveWorkspace(workspace.id, -1)),
      command('move-workspace-down', 'Descendre le workspace', () => store.moveWorkspace(workspace.id, 1)),
    )
  }
  if (tab) {
    const paneId = tab.active
    items.push(
      command('copy-path', 'Copier le chemin du pane actif', () => copyPanePath(paneId)),
      command('open-editor', 'Ouvrir le dossier du pane actif dans l’éditeur', () => openPaneFolder(paneId, OpenTarget.Editor)),
      command('open-explorer', 'Ouvrir le dossier du pane actif dans l’explorateur', () => openPaneFolder(paneId, OpenTarget.Explorer)),
      command('copy-branch', 'Copier la branche Git du pane actif', () => copyPaneBranch(paneId)),
    )
    items.push(
      command('rename-tab', 'Renommer l’onglet', () => ui.startRenamingTab(tab.id)),
      command('duplicate-tab', 'Dupliquer l’onglet', () => duplicateTabKeepingLayout(tab.id)),
      command('close-tab', 'Fermer l’onglet', () => closeTabKeepingText(tab.id)),
    )
    if (workspace && workspace.tabs.length > 1) {
      items.push(command('close-other-tabs', 'Fermer les autres onglets', () => closeOtherTabsKeepingText(tab.id)))
    }
    for (const target of session.workspaces.filter((candidate) => candidate.id !== workspace?.id)) {
      items.push(command(`move-tab-${target.id}`, `Déplacer l’onglet vers${SEPARATOR}${target.name}`, () => store.moveTab(tab.id, target.id)))
    }
  }
  return items
}

const attentionItems = (session: Session): PaletteItem[] =>
  waitingPanes(session, useAgentStore.getState().agents).map((pane) => ({
    id: `attention-${pane.paneId}`,
    kind: PaletteKind.Attention,
    label: `Rejoindre${SEPARATOR}${pane.label}`,
    hint: pane.detail,
    run: () => joinPane(pane.paneId),
  }))

const closedTabItems = (session: Session): PaletteItem[] =>
  session.closed
    .map((entry, position) => ({
      id: `closed-${position}-${entry.tab.id}`,
      kind: PaletteKind.Command,
      label: `Rouvrir l’onglet fermé${SEPARATOR}${entry.tab.name}`,
      hint: entry.workspaceName,
      run: () => restoreClosedTabAt(position),
    }))
    .reverse()

const navigationItems = (session: Session): PaletteItem[] => {
  const { selectWorkspace, selectTab, selectPane } = useSessionStore.getState()
  return session.workspaces.flatMap((workspace) => [
    { id: `ws-${workspace.id}`, kind: PaletteKind.Workspace, label: `Workspace${SEPARATOR}${workspace.name}`, run: () => selectWorkspace(workspace.id) },
    ...workspace.tabs.flatMap((tab) => [
      {
        id: `tab-${tab.id}`,
        kind: PaletteKind.Tab,
        label: `Onglet${SEPARATOR}${workspace.name} / ${tab.name}`,
        run: () => {
          selectWorkspace(workspace.id)
          selectTab(tab.id)
        },
      },
      ...panesOf(tab.tree).map((pane) => ({
        id: `pane-${pane.id}`,
        kind: PaletteKind.Pane,
        label: `Pane${SEPARATOR}${workspace.name} / ${tab.name} / ${folderName(pane.path)} (${pane.shell})`,
        hint: pane.path,
        run: () => selectPane(pane.id),
      })),
    ]),
  ])
}

export const toggleFavoriteCommand = (commandId: string): void => {
  const { session, toggleFavorite } = useSessionStore.getState()
  const { status, setStatus } = useHostStore.getState()
  const favorites = session?.favorites ?? []
  if (!favorites.includes(commandId) && favorites.length >= FAVORITES_MAX) {
    setStatus(FAVORITES_FULL_STATUS)
    return
  }
  toggleFavorite(commandId)
  if (status.text === FAVORITES_FULL_STATUS) {
    setStatus(NO_STATUS)
  }
}

export const buildPaletteItems = (session: Session, shells: ShellProfile[]): PaletteItem[] => {
  const commands = commandItems(session, shells).map((item) => ({ ...item, favorite: session.favorites.includes(item.id) }))
  const favorites = commands.filter((item) => item.favorite)
  const others = commands.filter((item) => !item.favorite)
  return [...attentionItems(session), ...favorites, ...others, ...closedTabItems(session), ...navigationItems(session)]
}

