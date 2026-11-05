import { beforeEach, describe, expect, it } from 'vitest'
import { createTab, createWorkspace, DEFAULT_GIT_GRAPH, EXPLORER_DEFAULT, SIDEBAR_DEFAULT, SplitAxis, panesOf, activeTab, type Session, type Workspace } from '../model/session'
import { useSessionStore } from './sessionStore'

const sessionOf = (workspaces: Workspace[]): Session => ({
  version: 1,
  workspaces,
  active: workspaces[0]?.id ?? '',
  sidebar: SIDEBAR_DEFAULT,
  sidebarCollapsed: false,
  explorerWidth: EXPLORER_DEFAULT,
  gitGraph: DEFAULT_GIT_GRAPH,
  closed: [],
  favorites: [],
})

const workspaceWithTabs = (name: string, tabCount: number): Workspace => {
  const workspace = createWorkspace(name, '/tmp/projet', 'zsh')
  for (let index = 1; index < tabCount; index++) {
    workspace.tabs.push(createTab(`/tmp/projet-${index}`, 'zsh'))
  }
  return workspace
}

const loaded = (workspaces: Workspace[]): void => useSessionStore.getState().load(sessionOf(workspaces))

const session = (): Session => {
  const current = useSessionStore.getState().session
  if (!current) {
    throw new Error('Session absente')
  }
  return current
}

describe('sessionStore', () => {
  beforeEach(() => {
    useSessionStore.setState({ session: null })
  })

  describe('closeTab et restoreTab', () => {
    it('ferme un onglet, le mémorise et active son voisin', () => {
      const workspace = workspaceWithTabs('A', 3)
      const [first, second, third] = workspace.tabs
      workspace.active = second!.id
      loaded([workspace])

      useSessionStore.getState().closeTab(second!.id)

      expect(session().workspaces[0]!.tabs.map((tab) => tab.id)).toEqual([first!.id, third!.id])
      expect(session().workspaces[0]!.active).toBe(third!.id)
      expect(session().closed).toHaveLength(1)
      expect(session().closed[0]).toMatchObject({ workspaceId: workspace.id, index: 1 })
    })

    it('rouvre l’onglet fermé à sa position avec de nouveaux identifiants', () => {
      const workspace = workspaceWithTabs('A', 3)
      const closedTab = workspace.tabs[1]!
      loaded([workspace])
      useSessionStore.getState().closeTab(closedTab.id)

      const restored = useSessionStore.getState().restoreTab()

      expect(restored).not.toBeNull()
      expect(restored!.tab.id).not.toBe(closedTab.id)
      expect(session().workspaces[0]!.tabs[1]!.id).toBe(restored!.tab.id)
      expect(session().workspaces[0]!.active).toBe(restored!.tab.id)
      expect(session().closed).toHaveLength(0)
    })

    it('supprime le workspace quand son dernier onglet est fermé et le recrée à la restauration avec sa note', () => {
      const kept = workspaceWithTabs('Gardé', 1)
      const emptied = workspaceWithTabs('Vidé', 1)
      emptied.note = 'à conserver'
      loaded([kept, emptied])
      useSessionStore.getState().selectWorkspace(emptied.id)

      useSessionStore.getState().closeTab(emptied.tabs[0]!.id)

      expect(session().workspaces.map((workspace) => workspace.id)).toEqual([kept.id])
      expect(session().active).toBe(kept.id)
      expect(session().closed[0]!.workspaceNote).toBe('à conserver')

      useSessionStore.getState().restoreTab()

      const recreated = session().workspaces.find((workspace) => workspace.id === emptied.id)
      expect(recreated).toMatchObject({ name: 'Vidé', note: 'à conserver' })
      expect(session().active).toBe(emptied.id)
    })

    it('ne garde que les derniers onglets fermés', () => {
      const workspace = workspaceWithTabs('A', 9)
      loaded([workspace])

      workspace.tabs.slice(0, 8).forEach((tab) => useSessionStore.getState().closeTab(tab.id))

      expect(session().closed).toHaveLength(5)
    })

    it('renvoie null sans onglet à rouvrir', () => {
      loaded([workspaceWithTabs('A', 1)])

      expect(useSessionStore.getState().restoreTab()).toBeNull()
    })
  })

  describe('moveTab', () => {
    it('fusionne la note d’un workspace vidé dans le workspace cible', () => {
      const source = workspaceWithTabs('Source', 1)
      source.note = 'note source'
      const target = workspaceWithTabs('Cible', 1)
      target.note = 'note cible'
      loaded([source, target])

      useSessionStore.getState().moveTab(source.tabs[0]!.id, target.id)

      expect(session().workspaces.map((workspace) => workspace.id)).toEqual([target.id])
      expect(session().workspaces[0]!.note).toBe('note cible\n\nSource :\nnote source')
      expect(session().active).toBe(target.id)
    })

    it('insère l’onglet avant l’onglet visé dans le même workspace', () => {
      const workspace = workspaceWithTabs('A', 3)
      const [first, second, third] = workspace.tabs
      loaded([workspace])

      useSessionStore.getState().moveTab(third!.id, workspace.id, first!.id)

      expect(session().workspaces[0]!.tabs.map((tab) => tab.id)).toEqual([third!.id, first!.id, second!.id])
    })

    it('ne fait rien quand l’onglet est déplacé avant lui-même', () => {
      const workspace = workspaceWithTabs('A', 2)
      loaded([workspace])
      const before = session()

      useSessionStore.getState().moveTab(workspace.tabs[0]!.id, workspace.id, workspace.tabs[0]!.id)

      expect(session()).toBe(before)
    })
  })

  describe('splitPane et closePane', () => {
    it('divise le pane actif et active le nouveau pane', () => {
      const workspace = workspaceWithTabs('A', 1)
      loaded([workspace])

      useSessionStore.getState().splitPane(SplitAxis.Horizontal)

      const tab = activeTab(session().workspaces[0]!)
      expect(panesOf(tab.tree)).toHaveLength(2)
      expect(tab.active).not.toBe(workspace.tabs[0]!.active)
    })

    it('retire un pane d’un onglet divisé et réactive le premier pane', () => {
      const workspace = workspaceWithTabs('A', 1)
      loaded([workspace])
      useSessionStore.getState().splitPane(SplitAxis.Horizontal)
      const activeId = activeTab(session().workspaces[0]!).active

      useSessionStore.getState().closePane(activeId)

      const tab = activeTab(session().workspaces[0]!)
      expect(panesOf(tab.tree).map((pane) => pane.id)).toEqual([workspace.tabs[0]!.active])
      expect(tab.active).toBe(workspace.tabs[0]!.active)
      expect(session().closed).toHaveLength(0)
    })

    it('ferme l’onglet quand son dernier pane est fermé', () => {
      const workspace = workspaceWithTabs('A', 2)
      loaded([workspace])
      const lastPaneId = workspace.tabs[1]!.active

      useSessionStore.getState().closePane(lastPaneId)

      expect(session().workspaces[0]!.tabs).toHaveLength(1)
      expect(session().closed).toHaveLength(1)
    })

    it('ignore un pane inconnu', () => {
      loaded([workspaceWithTabs('A', 1)])
      const before = session()

      useSessionStore.getState().closePane('inconnu')

      expect(session()).toBe(before)
    })
  })
})
