import { waitingPanes } from '../agents/agentSummary'
import { bridge } from '../bridge/bridge'
import type { ShellProfile } from '../bridge/messages'
import { Command, runCommand } from '../keyboard/shortcuts'
import { activeTab, activeWorkspace, panesOf, type Session } from '../model/session'
import { useAgentStore } from '../store/agentStore'
import { useSessionStore } from '../store/sessionStore'
import { RenameOrigin, useUiStore } from '../store/uiStore'
import { restoreClosedTab } from '../terminal/tabLifecycle'
import { terminalRegistry } from '../terminal/terminalRegistry'
import { OpenTarget } from '../bridge/messages'
import { copyPaneBranch, copyPanePath, openPaneFolder } from '../terminal/contextActions'
import type { SearchItem } from './searchFilter'

export enum PaletteKind {
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
    command('toggle-sidebar', session.sidebarCollapsed ? 'Afficher les workspaces' : 'Masquer les workspaces', store.toggleSidebar),
  ]
  if (workspace) {
    items.push(command('rename-workspace', 'Renommer le workspace', () => ui.startRenamingWorkspace(workspace.id, RenameOrigin.Header)))
  }
  if (tab) {
    const paneId = tab.active
    items.push(
      command('copy-path', 'Copier le chemin du pane actif', () => copyPanePath(paneId)),
      command('open-editor', 'Ouvrir le dossier du pane actif dans l’éditeur', () => openPaneFolder(paneId, OpenTarget.Editor)),
      command('open-explorer', 'Ouvrir le dossier du pane actif dans l’explorateur', () => openPaneFolder(paneId, OpenTarget.Explorer)),
      command('copy-branch', 'Copier la branche Git du pane actif', () => copyPaneBranch(paneId)),
    )
    items.push(command('rename-tab', 'Renommer l’onglet', () => ui.startRenamingTab(tab.id)))
    for (const target of session.workspaces.filter((candidate) => candidate.id !== workspace?.id)) {
      items.push(command(`move-tab-${target.id}`, `Déplacer l’onglet vers${SEPARATOR}${target.name}`, () => store.moveTab(tab.id, target.id)))
    }
  }
  return items
}

const attentionItems = (session: Session): PaletteItem[] => {
  const { selectPane } = useSessionStore.getState()
  return waitingPanes(session, useAgentStore.getState().agents).map((pane) => ({
    id: `attention-${pane.paneId}`,
    kind: PaletteKind.Attention,
    label: `Rejoindre${SEPARATOR}${pane.label}`,
    hint: pane.detail,
    favorite: false,
    run: () => {
      selectPane(pane.paneId)
      terminalRegistry.get(pane.paneId)?.terminal.focus()
    },
  }))
}

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
        label: `Pane${SEPARATOR}${workspace.name} / ${tab.name} / ${pane.shell}`,
        hint: pane.path,
        run: () => selectPane(pane.id),
      })),
    ]),
  ])
}

export const buildPaletteItems = (session: Session, shells: ShellProfile[]): PaletteItem[] => {
  const commands = commandItems(session, shells).map((item) => ({ ...item, favorite: session.favorites.includes(item.id) }))
  const favorites = commands.filter((item) => item.favorite)
  const others = commands.filter((item) => !item.favorite)
  return [...attentionItems(session), ...favorites, ...others, ...navigationItems(session)]
}

