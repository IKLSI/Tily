import { useShallow } from 'zustand/react/shallow'
import type { Project } from '../../../bridge/messages'
import { useHostStore } from '../../../stores/hostStore'
import { useWorktreeStore, WorktreePickerKind } from '../worktreeStore'
import { closeWorktreePicker, dialogCreationPending, openProjectWorktreeDialog, openWorktree } from '../worktreeActions'
import { WorktreeDialog } from './WorktreeDialog'
import { WorktreePicker } from './WorktreePicker'
import { WorktreeRemoveDialog } from './WorktreeRemoveDialog'

const handleSelect = (kind: WorktreePickerKind, project: Project, inActiveWorkspace: boolean): void => {
  if (kind === WorktreePickerKind.Source) {
    openProjectWorktreeDialog(project.path)
    return
  }
  useWorktreeStore.getState().setPicker(null)
  openWorktree(project.path, inActiveWorkspace)
}

export function WorktreeDialogs() {
  const { picker, draft, sources, plan, planPending, creating, createFailure, removal } = useWorktreeStore(
    useShallow((state) => ({
      picker: state.picker,
      draft: state.draft,
      sources: state.sources,
      plan: state.plan,
      planPending: state.planPending,
      creating: dialogCreationPending(state.tasks),
      createFailure: state.createFailure,
      removal: state.removal,
    })),
  )
  const { projects, projectsRoot, projectsError } = useHostStore(useShallow((state) => ({ projects: state.projects, projectsRoot: state.projectsRoot, projectsError: state.projectsError })))

  return (
    <>
      {picker && <WorktreePicker kind={picker} projects={projects} root={projectsRoot} error={projectsError} onClose={closeWorktreePicker} onSelect={handleSelect} />}
      {draft && <WorktreeDialog draft={draft} sources={sources} plan={plan} planPending={planPending} busy={creating} failure={createFailure} />}
      {removal && <WorktreeRemoveDialog removal={removal} />}
    </>
  )
}
