export interface AccentPreset {
  name: string
  color: string
}

export interface TerminalTheme {
  background: string
  foreground: string
  cursor: string
  selectionBackground: string
}

export interface AccentScheme {
  tokens: Record<string, string>
  terminal: TerminalTheme
}

export interface AccentTheme extends AccentScheme {
  accent: string
  light: AccentScheme
}

export interface AppearanceState {
  theme: AccentTheme
  defaultColor: string
}

export interface FolderProject {
  path: string
  project?: string
  projectName?: string
  color?: string
}

export interface AppearanceCatalog {
  state: AppearanceState
  accentPresets: AccentPreset[]
}

export type AppearanceHostMessage =
  | { type: 'appearance.theme'; state: AppearanceState }
  | { type: 'appearance.projectsResolved'; projects: FolderProject[] }

export type AppearanceWebMessage =
  | { type: 'appearance.projects'; paths: string[] }
  | { type: 'appearance.preview'; color?: string }
  | { type: 'appearance.projectColor'; project: string; color?: string }
