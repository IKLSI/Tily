interface GitAheadBehindProps {
  ahead: number
  behind: number
  tip?: string
}

export function GitAheadBehind({ ahead, behind, tip }: GitAheadBehindProps) {
  return (
    <span className="flex shrink-0 gap-[4px] font-mono text-[11px]" data-tip={tip}>
      <span className={ahead > 0 ? 'text-dock-diff-added-ink' : 'text-dock-muted'}>{`↑${ahead}`}</span>
      <span className={behind > 0 ? 'text-dock-diff-removed-ink' : 'text-dock-muted'}>{`↓${behind}`}</span>
    </span>
  )
}
