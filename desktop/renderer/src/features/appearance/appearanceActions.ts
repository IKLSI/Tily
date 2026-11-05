import { bridge } from '../../bridge/bridge'
import type { ITheme } from '@xterm/xterm'
import type { AccentScheme, AppearanceCatalog, AppearanceState, FolderProject } from '../../bridge/appearanceMessages'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { activePane, activeTab, type Session, type Workspace } from '../../model/session'
import { useAppearanceStore, type ColorTarget } from '../../stores/appearanceStore'
import { useSessionStore } from '../../stores/sessionStore'
import { terminalRegistry } from '../terminal/terminalRegistry'

const TOKEN_PREFIX = '--color-tily-'
const PROJECT_COLOR_DELAY_MS = 300

const projectColorTimers = new Map<string, ReturnType<typeof setTimeout>>()

export const workspacePath = (workspace: Workspace): string => activePane(activeTab(workspace)).path

const followedPaths = (session: Session | null): string[] => [...new Set((session?.workspaces ?? []).map(workspacePath))].sort()

const LIGHT_ANSI: ITheme = {
  black: '#1d1d1f',
  red: '#c4261d',
  green: '#1f8a3b',
  yellow: '#8a6d00',
  blue: '#0b5fd1',
  magenta: '#a3329b',
  cyan: '#0c7c8c',
  white: '#8e8e93',
  brightBlack: '#636366',
  brightRed: '#e0352b',
  brightGreen: '#248a3d',
  brightYellow: '#a07800',
  brightBlue: '#0a6cf0',
  brightMagenta: '#bf3fb5',
  brightCyan: '#0e8fa2',
  brightWhite: '#48484a',
}

const scheme = window.matchMedia('(prefers-color-scheme: light)')
let currentState: AppearanceState | null = null

const terminalTheme = (colors: AccentScheme, light: boolean): ITheme => ({
  ...colors.terminal,
  ...(light ? LIGHT_ANSI : {}),
  scrollbarSliderBackground: colors.tokens['scrollbar'],
  scrollbarSliderHoverBackground: colors.tokens['scrollbar-hover'],
  scrollbarSliderActiveBackground: colors.tokens['scrollbar-hover'],
})

const applyScheme = (): void => {
  if (!currentState) {
    return
  }
  const light = currentState.colorScheme === 'system' ? scheme.matches : currentState.colorScheme === 'light'
  const colors = light ? currentState.theme.light : currentState.theme
  const root = document.documentElement.style
  document.documentElement.style.colorScheme = light ? 'light' : 'dark'
  Object.entries(colors.tokens).forEach(([token, color]) => root.setProperty(`${TOKEN_PREFIX}${token}`, color))
  terminalRegistry.setTheme(terminalTheme(colors, light))
}

scheme.addEventListener('change', applyScheme)

export const receiveAppearance = (state: AppearanceState): void => {
  currentState = state
  applyScheme()
  useAppearanceStore.getState().setState(state)
}

export const receiveAppearanceCatalog = (catalog: AppearanceCatalog): void => {
  useAppearanceStore.getState().setAccentPresets(catalog.accentPresets)
  receiveAppearance(catalog.state)
}

export const receiveProjects = (projects: FolderProject[]): void => useAppearanceStore.getState().setProjects(projects)

export const startAppearanceProjects = (): (() => void) => {
  let sent: string | null = null
  return useSessionStore.subscribe((state) => {
    const paths = followedPaths(state.session)
    const key = paths.join('\n')
    if (key !== sent) {
      sent = key
      bridge.send({ type: 'appearance.projects', paths })
    }
  })
}

export const setWorkspaceColor = (workspaceId: string, color: string | undefined): void => useSessionStore.getState().setWorkspaceColor(workspaceId, color)

export const setTabColor = (tabId: string, color: string | undefined): void => useSessionStore.getState().setTabColor(tabId, color)

export const setProjectColor = (project: string, color: string | undefined): void => {
  useAppearanceStore.setState((store) => ({
    projects: Object.fromEntries(Object.entries(store.projects).map(([key, folder]) => [key, folder.project === project ? { ...folder, color } : folder])),
  }))
  clearTimeout(projectColorTimers.get(project))
  projectColorTimers.set(
    project,
    setTimeout(() => {
      projectColorTimers.delete(project)
      bridge.send({ type: 'appearance.projectColor', project, color })
    }, PROJECT_COLOR_DELAY_MS),
  )
}

export const openColorDialog = (target: ColorTarget): void => useAppearanceStore.getState().openColorDialog(target)

export const openTabColor = (tabId: string): void => {
  const workspace = useSessionStore.getState().session?.workspaces.find((candidate) => candidate.tabs.some((tab) => tab.id === tabId))
  if (workspace) {
    openColorDialog({ workspaceId: workspace.id, tabId })
  }
}

export const closeColorDialog = (): void => {
  useAppearanceStore.getState().closeColorDialog()
  focusActivePane()
}
