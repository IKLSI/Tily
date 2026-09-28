import type { Session } from '../model/session'
import type { GitHostMessage, GitWebMessage } from './gitMessages'

export interface ShellProfile {
  id: string
  name: string
  executable: string
  arguments: string
  available: boolean
  reportsCurrentDirectory: boolean
}

export enum PickTarget {
  File = 'file',
  Folder = 'folder',
  Sound = 'sound',
}

export interface PickedPath {
  field: string
  path: string
}

export enum OpenTarget {
  Editor = 'editor',
  Explorer = 'explorer',
}

export enum EntryKind {
  File = 'file',
  Folder = 'folder',
}

export interface FileEntry {
  name: string
  path: string
  isDirectory: boolean
}

export interface GitContext {
  isRepository: boolean
  branch: string | null
  detachedHead: boolean
}

export interface PersistenceSettings {
  textIntervalSeconds: number
  linesPerPane: number
  maxTextMebibytes: number
}

export enum NotificationSound {
  None = 'none',
  Default = 'Notification.Default',
  InstantMessage = 'Notification.IM',
  Mail = 'Notification.Mail',
  Reminder = 'Notification.Reminder',
  Sms = 'Notification.SMS',
}

export interface NotificationSettings {
  windowsToast: boolean
  sound: NotificationSound | string
  taskbarFlash: boolean
}

export interface Settings {
  shells: Record<string, string>
  editor: string
  persistence: PersistenceSettings
  projectsRoot: string
  notifications: NotificationSettings
}

export interface ShellSetting {
  id: string
  name: string
  defaultExecutable: string
  configured: string
  available: boolean
}

export interface ImportedPreferences {
  settings: Settings
  path: string
}

export interface AgentHooksInfo {
  script: string
  stateDirectory: string
  settingsFile: string
  hooksInstalled: boolean
}

export interface NotificationAvailability {
  toastAvailable: boolean
  toastError?: string
}

export interface SettingsSnapshot {
  settings: Settings
  shellSettings: ShellSetting[]
  files: Record<string, string>
  warnings: string[]
  agents: AgentHooksInfo
  notifications: NotificationAvailability
}

export interface Project {
  name: string
  path: string
}

export interface PaneActivity {
  paneId: string
  processes: string[]
}

export enum AgentState {
  Working = 'working',
  Waiting = 'waiting',
  Done = 'done',
  Error = 'error',
  Unknown = 'unknown',
}

export interface PaneAgent {
  paneId: string
  agent: string
  state: AgentState
  message?: string
}

export type HostToWebMessage =
  | { type: 'app.hello'; session: Session; shells: ShellProfile[]; home: string; text: Record<string, string>; persistence: PersistenceSettings; recovery?: string }
  | { type: 'app.closing'; activity: PaneActivity[] }
  | { type: 'session.saved' }
  | { type: 'session.saveFailed'; message: string }
  | ({ type: 'dialog.picked' } & PickedPath)
  | ({ type: 'settings.result'; shells: ShellProfile[]; persistence: PersistenceSettings; saved: boolean } & SettingsSnapshot)
  | { type: 'settings.exported'; path: string }
  | ({ type: 'settings.imported' } & ImportedPreferences)
  | { type: 'terminal.created'; pane: string; pid: number }
  | { type: 'terminal.output'; pane: string; data: string }
  | { type: 'terminal.cwd'; pane: string; path: string }
  | { type: 'terminal.exit'; pane: string; code: number }
  | { type: 'terminal.pathMissing'; pane: string; path: string; fallback: string }
  | { type: 'terminal.activityResult'; panes: PaneActivity[] }
  | { type: 'agent.states'; panes: PaneAgent[] }
  | { type: 'agent.join'; pane: string }
  | { type: 'projects.listed'; root: string; projects: Project[]; error?: string }
  | { type: 'context.result'; pane: string; path: string; git: GitContext }
  | { type: 'files.listed'; path: string; entries: FileEntry[]; total: number; error?: string }
  | { type: 'files.created'; path: string }
  | { type: 'files.renamed'; path: string; target: string }
  | { type: 'files.deleted'; path: string }
  | { type: 'error'; pane?: string; message: string }
  | GitHostMessage

export type WebToHostMessage =
  | { type: 'app.ready' }
  | { type: 'session.save'; session: Session }
  | { type: 'text.save'; text: Record<string, string>; keep: string[] }
  | { type: 'settings.get' }
  | { type: 'settings.save'; settings: Settings }
  | { type: 'settings.export' }
  | { type: 'settings.import' }
  | { type: 'attention.raise'; pane: string; title: string; body: string }
  | { type: 'attention.test'; pane: string; notifications: NotificationSettings }
  | { type: 'agents.installHooks' }
  | { type: 'agents.removeHooks' }
  | { type: 'dialog.pick'; field: string; target: PickTarget }
  | { type: 'terminal.create'; pane: string; shell: string; cwd: string; cols: number; rows: number }
  | { type: 'terminal.input'; pane: string; data: string }
  | { type: 'terminal.resize'; pane: string; cols: number; rows: number }
  | { type: 'terminal.ack'; pane: string; chars: number }
  | { type: 'terminal.close'; pane: string }
  | { type: 'terminal.activity'; panes: string[] }
  | { type: 'projects.list' }
  | { type: 'context.query'; pane: string; path: string }
  | { type: 'context.open'; pane: string; path: string; target: OpenTarget }
  | { type: 'link.open'; url: string }
  | { type: 'files.watch'; paths: string[] }
  | { type: 'files.refresh' }
  | { type: 'files.open'; path: string }
  | { type: 'files.create'; path: string; name: string; kind: EntryKind }
  | { type: 'files.rename'; path: string; parent: string; name: string }
  | { type: 'files.delete'; path: string; parent: string }
  | { type: 'window.close' }
  | { type: 'window.closeCancel' }
  | GitWebMessage

export type HostMessageType = HostToWebMessage['type']
export type HostMessageOf<T extends HostMessageType> = Extract<HostToWebMessage, { type: T }>
