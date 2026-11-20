import { useShallow } from 'zustand/react/shallow'
import { cloneFromDialogPending, useCloneStore } from '../cloneStore'
import { CloneDialog } from './CloneDialog'

export function CloneDialogs() {
  const { draft, plan, planPending, roots, busy, progress, failure } = useCloneStore(
    useShallow((state) => ({
      draft: state.draft,
      plan: state.plan,
      planPending: state.planPending,
      roots: state.roots,
      busy: cloneFromDialogPending(state.tasks),
      progress: state.progress,
      failure: state.failure,
    })),
  )

  return draft ? <CloneDialog draft={draft} plan={plan} planPending={planPending} roots={roots} busy={busy} progress={progress} failure={failure} /> : null
}
