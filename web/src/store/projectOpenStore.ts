import { create } from 'zustand'
import type { Project } from '../bridge/messages'
import type { WorktreeSources } from '../bridge/worktreeMessages'

export interface ProjectChoice {
  project: Project
  inActiveWorkspace: boolean
  request: number
  sources: WorktreeSources | null
}

export interface ProjectBrowse {
  inActiveWorkspace: boolean
  remember: boolean
}

interface ProjectOpenState {
  request: number
  choice: ProjectChoice | null
  browse: ProjectBrowse | null
  nextRequest: () => number
  setChoice: (choice: ProjectChoice | null) => void
  setBrowse: (browse: ProjectBrowse | null) => void
}

export const useProjectOpenStore = create<ProjectOpenState>()((set, get) => ({
  request: 0,
  choice: null,
  browse: null,
  nextRequest: () => {
    const request = get().request + 1
    set({ request })
    return request
  },
  setChoice: (choice) => set(choice ? { choice } : { choice, browse: null }),
  setBrowse: (browse) => set({ browse }),
}))
