import { create } from 'zustand'
import type { WorktreeBranchMode, WorktreeOperation, WorktreePlan } from '../bridge/worktreeMessages'

export enum WorktreePickerKind {
  Source = 'source',
  Open = 'open',
}

export interface WorktreeDraft {
  repository: string
  branch: string
  mode: WorktreeBranchMode
  base: string
  install: boolean
  database: boolean
}

export interface WorktreeRemovalPane {
  paneId: string
  label: string
  agent: boolean
}

export interface WorktreeFailure {
  message: string
  output?: string
  lockedBy?: string[]
}

export interface WorktreeRemoval {
  path: string
  name: string
  branch?: string
  panes: WorktreeRemovalPane[]
  closePanes: boolean
  keepBranch: boolean
  dropDatabase: boolean
  failure: WorktreeFailure | null
}

interface WorktreeState {
  picker: WorktreePickerKind | null
  draft: WorktreeDraft | null
  planRequest: number
  planPending: boolean
  plan: WorktreePlan | null
  createFailure: WorktreeFailure | null
  busy: WorktreeOperation | null
  removal: WorktreeRemoval | null
  pendingRemoval: WorktreeRemoval | null
  setPicker: (picker: WorktreePickerKind | null) => void
  setDraft: (draft: WorktreeDraft | null) => void
  requestPlan: () => number
  receivePlan: (request: number, plan: WorktreePlan) => void
  setCreateFailure: (createFailure: WorktreeFailure | null) => void
  setBusy: (busy: WorktreeOperation | null) => void
  setRemoval: (removal: WorktreeRemoval | null) => void
  setPendingRemoval: (pendingRemoval: WorktreeRemoval | null) => void
}

export const useWorktreeStore = create<WorktreeState>()((set, get) => ({
  picker: null,
  draft: null,
  planRequest: 0,
  planPending: false,
  plan: null,
  createFailure: null,
  busy: null,
  removal: null,
  pendingRemoval: null,
  setPicker: (picker) => set({ picker }),
  setDraft: (draft) => set(draft ? { draft } : { draft, plan: null, planPending: false, createFailure: null }),
  requestPlan: () => {
    const planRequest = get().planRequest + 1
    set({ planRequest, planPending: true })
    return planRequest
  },
  receivePlan: (request, plan) => set((current) => (current.planRequest === request && current.draft ? { plan, planPending: false } : current)),
  setCreateFailure: (createFailure) => set({ createFailure }),
  setBusy: (busy) => set({ busy }),
  setRemoval: (removal) => set({ removal }),
  setPendingRemoval: (pendingRemoval) => set({ pendingRemoval }),
}))

export const worktreeModalOpen = (): boolean => {
  const { picker, draft, removal } = useWorktreeStore.getState()
  return picker !== null || draft !== null || removal !== null
}
