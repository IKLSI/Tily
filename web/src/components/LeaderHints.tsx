import { LEADER_HINTS } from '../keyboard/leaderHints'

export function LeaderHints() {
  return (
    <div role="status" aria-live="polite" className="flex min-w-0 flex-1 items-center text-xs">
      <span className="shrink-0 rounded bg-dock-focus px-2 py-0.5 font-semibold text-dock-terminal">Leader…</span>
      <ul className="absolute top-full left-3 z-40 mt-1 grid w-[min(860px,calc(100%-24px))] grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-x-4 gap-y-1 rounded-md border border-dock-line bg-dock-panel px-3 py-2 leading-[18px] text-dock-muted shadow-2xl">
        {LEADER_HINTS.map((hint) => (
          <li key={hint.keys} className="min-w-0 truncate">
            <kbd className="rounded border border-dock-line bg-dock-paper px-1 font-mono text-[11px] text-dock-ink">{hint.keys}</kbd> {hint.label}
          </li>
        ))}
      </ul>
    </div>
  )
}
