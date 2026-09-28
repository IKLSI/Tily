import { RightPanelView } from '../model/session'
import { showPanelView } from '../panel/rightPanel'
import { FileExplorer } from './FileExplorer'
import { GitPanel } from './GitPanel'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { PANEL_HEADER_BUTTON } from './rightPanelStyles'

interface RightPanelProps {
  view: RightPanelView
  root: string
  width: number
  onClose: () => void
  onOpenTerminal: (path: string) => void
}

const VIEWS: { view: RightPanelView; label: string; shortcut: string }[] = [
  { view: RightPanelView.Files, label: 'Fichiers', shortcut: 'Ctrl + Maj + E' },
  { view: RightPanelView.Git, label: 'Git', shortcut: 'Ctrl + Maj + G' },
]

export function RightPanel({ view, root, width, onClose, onOpenTerminal }: RightPanelProps) {
  return (
    <aside data-right-panel="" aria-label="Panneau de droite" className="flex h-full min-h-0 shrink-0 flex-col overflow-hidden bg-dock-paper" style={{ width }}>
      <div role="tablist" aria-label="Vue du panneau" className="flex h-[36px] shrink-0 items-center gap-[2px] pr-[6px] pl-[8px]">
        {VIEWS.map((entry) => {
          const selected = entry.view === view
          const handleSelect = () => showPanelView(entry.view)
          return (
            <button
              key={entry.view}
              type="button"
              role="tab"
              aria-selected={selected}
              data-tip={`${entry.label} (${entry.shortcut})`}
              className={`cursor-pointer rounded-md px-[8px] py-[3px] text-[11px] font-semibold tracking-[0.06em] uppercase ${selected ? 'bg-dock-green-soft text-dock-green-deep' : 'text-dock-muted hover:bg-dock-green-hover hover:text-dock-ink'}`}
              onClick={handleSelect}
            >
              {entry.label}
            </button>
          )
        })}
        <span className="flex-1" />
        <button type="button" className={PANEL_HEADER_BUTTON} aria-label="Masquer le panneau" data-tip="Masquer le panneau" onClick={onClose}>
          <Icon name={IconName.Close} />
        </button>
      </div>
      {view === RightPanelView.Git ? <GitPanel folder={root} /> : <FileExplorer root={root} onOpenTerminal={onOpenTerminal} />}
    </aside>
  )
}
