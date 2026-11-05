import { createWorkspace, findWorkspace, NOTE_MAX_CHARS } from '../../model/session'
import { mutateSession } from './helpers'
import type { SessionSlice, WorkspaceSlice } from './types'

export const createWorkspaceSlice: SessionSlice<WorkspaceSlice> = (set) => ({
  toggleWorkspace: (workspaceId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = draft.workspaces.find((candidate) => candidate.id === workspaceId)
        if (workspace) {
          workspace.expanded = !(workspace.expanded ?? workspace.id === draft.active)
        }
      }),
    })),

  collapseOtherWorkspaces: (workspaceId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        for (const workspace of draft.workspaces) {
          if (workspace.id !== workspaceId && (workspace.expanded ?? workspace.id === draft.active)) {
            workspace.expanded = false
          }
        }
      }),
    })),

  newWorkspace: (name, path, shell) => {
    const workspace = createWorkspace(name, path, shell)
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        draft.workspaces.push(workspace)
        draft.active = workspace.id
      }),
    }))
    return workspace.id
  },

  renameWorkspace: (workspaceId, name) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = findWorkspace(draft, workspaceId)
        const trimmed = name.trim()
        if (workspace && trimmed.length > 0) {
          workspace.name = trimmed
        }
      }),
    })),

  setWorkspaceNote: (workspaceId, note) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = findWorkspace(draft, workspaceId)
        if (workspace) {
          workspace.note = note.slice(0, NOTE_MAX_CHARS) || undefined
        }
      }),
    })),

  setWorkspaceColor: (workspaceId, color) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const workspace = findWorkspace(draft, workspaceId)
        if (workspace) {
          workspace.color = color
        }
      }),
    })),

  moveWorkspace: (workspaceId, offset) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const index = draft.workspaces.findIndex((workspace) => workspace.id === workspaceId)
        const destination = index + offset
        if (index < 0 || destination < 0 || destination >= draft.workspaces.length) {
          return
        }
        draft.workspaces.splice(destination, 0, ...draft.workspaces.splice(index, 1))
      }),
    })),

  moveWorkspaceBefore: (workspaceId, beforeWorkspaceId) =>
    set((state) => ({
      session: mutateSession(state.session, (draft) => {
        const index = draft.workspaces.findIndex((workspace) => workspace.id === workspaceId)
        const before = beforeWorkspaceId ? draft.workspaces.findIndex((workspace) => workspace.id === beforeWorkspaceId) : draft.workspaces.length
        if (index < 0 || before < 0 || before === index || before === index + 1) {
          return
        }
        draft.workspaces.splice(before > index ? before - 1 : before, 0, ...draft.workspaces.splice(index, 1))
      }),
    })),
})
