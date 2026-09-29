import type { WorktreeFailure } from '../store/worktreeStore'

interface WorktreeFailureDetailsProps {
  failure: WorktreeFailure
}

export function WorktreeFailureDetails({ failure }: WorktreeFailureDetailsProps) {
  const { message, output, lockedBy } = failure
  return (
    <div role="alert" className="flex flex-col gap-2 rounded border border-dock-error px-3 py-2">
      <p className="text-[12px] text-dock-error">{message}</p>
      {lockedBy && lockedBy.length > 0 && <p className="text-[11px] text-dock-ink">{`Processus qui verrouillent le dossier : ${lockedBy.join(', ')}.`}</p>}
      {output && <pre className="max-h-[160px] overflow-auto font-mono text-[11px] whitespace-pre-wrap text-dock-muted">{output}</pre>}
    </div>
  )
}
