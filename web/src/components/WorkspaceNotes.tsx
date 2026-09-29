import type { ChangeEvent, KeyboardEvent } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { activeWorkspace, NOTE_MAX_CHARS } from '../model/session'
import { useSessionStore } from '../store/sessionStore'
import { SECTION_TITLE } from './rightPanelStyles'

const NOTE_ID = 'workspace-note'

export function WorkspaceNotes() {
  const { id, name, note } = useSessionStore(
    useShallow((state) => {
      const workspace = state.session ? activeWorkspace(state.session) : undefined
      return { id: workspace?.id, name: workspace?.name ?? '', note: workspace?.note ?? '' }
    }),
  )

  const handleChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    if (id) {
      useSessionStore.getState().setWorkspaceNote(id, event.target.value)
    }
  }
  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Escape') {
      event.preventDefault()
      focusActivePane()
    }
  }

  return (
    <section aria-label="Notes du workspace" className="flex min-h-0 flex-1 flex-col px-[12px] pb-[12px]">
      <label htmlFor={NOTE_ID} className={`flex h-[30px] shrink-0 items-center ${SECTION_TITLE}`}>
        <span className="min-w-0 truncate" data-tip={name}>
          {name}
        </span>
      </label>
      <textarea
        id={NOTE_ID}
        data-workspace-note=""
        value={note}
        maxLength={NOTE_MAX_CHARS}
        spellCheck={false}
        placeholder="Notes de ce workspace : tâches, ports, commandes… Enregistrées avec la session."
        className="min-h-0 flex-1 resize-none rounded border border-dock-line bg-dock-panel px-[10px] py-[8px] font-mono text-[12px] leading-[1.5] text-dock-ink outline-none placeholder:text-dock-muted focus:border-dock-focus"
        onChange={handleChange}
        onKeyDown={handleKeyDown}
      />
    </section>
  )
}
