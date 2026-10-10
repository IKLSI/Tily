import type { WorktreeFailure } from '../worktreeStore'

interface WorktreeFailureDetailsProps {
  failure: WorktreeFailure
}

export function WorktreeFailureDetails({ failure }: WorktreeFailureDetailsProps) {
  const { message, output } = failure
  return (
    <div role="alert" className="flex flex-col gap-2 rounded border border-tily-error px-3 py-2">
      <p className="text-[12px] text-tily-error">{message}</p>
      {output && <pre className="max-h-[160px] overflow-auto font-mono text-[11px] whitespace-pre-wrap text-tily-muted">{output}</pre>}
    </div>
  )
}
