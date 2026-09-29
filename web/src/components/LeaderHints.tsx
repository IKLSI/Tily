import { LEADER_HINTS } from '../keyboard/shortcuts'

export function LeaderHints() {
  return (
    <div role="status" aria-live="polite" className="flex min-w-0 flex-1 items-center gap-2 overflow-hidden text-xs">
      <span className="shrink-0 rounded bg-dock-focus px-2 py-0.5 font-semibold text-dock-terminal">Leader…</span>
      <ul className="flex max-h-[40px] min-w-0 flex-wrap content-center items-center gap-x-3 gap-y-0.5 overflow-hidden leading-[18px] text-dock-muted">
        {LEADER_HINTS.map((hint) => (
          <li key={hint.keys} className="shrink-0 whitespace-nowrap">
            <kbd className="rounded border border-dock-line bg-dock-paper px-1 font-mono text-[11px] text-dock-ink">{hint.keys}</kbd> {hint.label}
          </li>
        ))}
      </ul>
    </div>
  )
}
