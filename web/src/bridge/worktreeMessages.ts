export enum WorktreeBranchMode {
  New = 'new',
  Local = 'local',
  Remote = 'remote',
}

export enum WorktreeOperation {
  Create = 'create',
  Remove = 'remove',
}

export interface Worktree {
  path: string
  branch?: string
  head?: string
  isMain: boolean
  isDetached: boolean
  locked: boolean
  prunable: boolean
}

export interface WorktreePlan {
  repository: string
  project: string
  path?: string
  localBranch?: string
  defaultBase?: string
  localBranches: string[]
  remoteBranches: string[]
  error?: string
  configuredBase?: string
}

export interface WorktreeSources {
  project: string
  repositories: string[]
  selected?: string
  defaultRepository?: string
}

export interface WorktreeSettings {
  folder: string
  defaultBase: string
}

export type WorktreeHostMessage =
  | { type: 'worktrees.sourcesFound'; request: number; sources: WorktreeSources }
  | { type: 'worktrees.planned'; request: number; plan: WorktreePlan }
  | { type: 'worktrees.progress'; operation: WorktreeOperation; message: string }
  | { type: 'worktrees.created'; path: string; name: string; branch: string; install?: string }
  | { type: 'worktrees.done'; operation: WorktreeOperation; message: string; warnings: string[] }
  | { type: 'worktrees.failed'; operation: WorktreeOperation; step: string; message: string; output?: string; lockedBy?: string[] }

export type WorktreeWebMessage =
  | { type: 'worktrees.sources'; request: number; path?: string; project?: string }
  | { type: 'worktrees.plan'; request: number; repository: string; branch: string; mode: WorktreeBranchMode; base?: string }
  | { type: 'worktrees.create'; repository: string; branch: string; mode: WorktreeBranchMode; base?: string; install: boolean; database: boolean; project: string; remember: boolean }
  | { type: 'worktrees.remove'; path: string; keepBranch: boolean; dropDatabase: boolean; confirmed: boolean }
