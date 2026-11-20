import { useState } from 'react'
import type { ReleaseHistoryEntry } from '../../../bridge/updateMessages'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { formatDate, openReleaseHistoryPage } from '../../updates/updateActions'
import { ReleaseNote } from '../../updates/components/ReleaseNote'
import { SETTINGS_HINT } from './settingsStyles'

interface ReleaseHistoryItemProps {
  entry: ReleaseHistoryEntry
  installed: boolean
  initiallyOpen: boolean
}

export function ReleaseHistoryItem({ entry, installed, initiallyOpen }: ReleaseHistoryItemProps) {
  const [open, setOpen] = useState(initiallyOpen)
  const handleToggle = () => setOpen((value) => !value)
  const handleOpenPage = () => openReleaseHistoryPage(entry)

  return (
    <div className="rounded-tily-sm border border-tily-line bg-tily-paper">
      <div className="flex items-center gap-2 px-3 py-2">
        <button type="button" aria-expanded={open} className="flex min-w-0 flex-1 cursor-pointer items-center gap-2 text-left" onClick={handleToggle}>
          <Icon name={IconName.Chevron} className={open ? 'rotate-90' : undefined} />
          <span className="text-tily-sm font-semibold text-tily-ink">{`Tily ${entry.version}`}</span>
          {installed && <span className="rounded-tily-sm bg-tily-green-soft px-1.5 text-tily-xs text-tily-green-deep">Installée</span>}
          {entry.publishedAt && <span className={SETTINGS_HINT}>{formatDate(entry.publishedAt)}</span>}
        </button>
        <button type="button" className="cursor-pointer text-tily-xs text-tily-green hover:underline" onClick={handleOpenPage}>
          Voir sur GitHub
        </button>
      </div>
      {open && (
        <div className="flex flex-col gap-3 border-t border-tily-line px-3 py-3">
          {entry.sections.length === 0 && <p className={SETTINGS_HINT}>Aucune note de version.</p>}
          {entry.sections.map((section, index) => (
            <div key={`${section.title ?? ''}-${index}`} className="flex flex-col gap-1.5">
              {section.title && <h4 className="text-tily-sm font-semibold text-tily-ink">{section.title}</h4>}
              <ul className="flex list-disc flex-col gap-1.5 pl-5 text-tily-sm text-tily-ink marker:text-tily-green">
                {section.notes.map((note) => (
                  <li key={note}>
                    <ReleaseNote text={note} />
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
