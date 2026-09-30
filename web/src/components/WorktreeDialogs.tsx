import { useShallow } from 'zustand/react/shallow'
import type { Project } from '../bridge/messages'
import { WorktreeOperation } from '../bridge/worktreeMessages'
import { useHostStore } from '../store/hostStore'
import { useWorktreeStore, WorktreePickerKind } from '../store/worktreeStore'
import { closeWorktreePicker, openWorktree, openWorktreeDialog } from '../worktree/worktreeActions'
import { WorktreeDialog } from './WorktreeDialog'
import { WorktreePicker } from './WorktreePicker'
import { WorktreeRemoveDialog } from './WorktreeRemoveDialog'

const handleSelect = (kind: WorktreePickerKind, project: Project, inActiveWorkspace: boolean): void => {
  if (kind === WorktreePickerKind.Source) {
    openWorktreeDialog(project.path)
    return
  }
  useWorktreeStore.getState().setPicker(null)
  openWorktree(project.path, inActiveWorkspace)
}

export function WorktreeDialogs() {
  const { picker, draft, plan, planPending, busy, createFailure, removal } = useWorktreeStore(
    useShallow((state) => ({
      picker: state.picker,
      draft: state.draft,
      plan: state.plan,
      planPending: state.planPending,
      busy: state.busy,
      createFailure: state.createFailure,
      removal: state.removal,
    })),
  )
  const { projects, projectsRoot, projectsError } = useHostStore(useShallow((state) => ({ projects: state.projects, projectsRoot: state.projectsRoot, projectsError: state.projectsError })))

  return (
    <>
      {picker && <WorktreePicker kind={picker} projects={projects} root={projectsRoot} error={projectsError} onClose={closeWorktreePicker} onSelect={handleSelect} />}
      {draft && <WorktreeDialog draft={draft} plan={plan} planPending={planPending} busy={busy === WorktreeOperation.Create} failure={createFailure} />}
      {removal && <WorktreeRemoveDialog removal={removal} />}
    </>
  )
}
