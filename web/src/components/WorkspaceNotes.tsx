import type { ChangeEvent, KeyboardEvent } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { focusActivePane } from '../explorer/fileExplorerActions'
import { activeWorkspace, NOTE_MAX_CHARS } from '../model/session'
import { useSessionStore } from '../store/sessionStore'
import { sendTextToActivePane } from '../terminal/terminalActions'
import { SECTION_TITLE } from './rightPanelStyles'

const NOTE_ID = 'workspace-note'
const NOTE_HINT_ID = 'workspace-note-hint'
const LINE_BREAK = '\n'
const TRAILING_LINE_BREAKS = /[\r\n]+$/

const noteTextToSend = (value: string, start: number, end: number): string => {
  if (start !== end) {
    return value.slice(start, end).replace(TRAILING_LINE_BREAKS, '')
  }
  const lineStart = value.lastIndexOf(LINE_BREAK, start - 1) + 1
  const lineEnd = value.indexOf(LINE_BREAK, start)
  return value.slice(lineStart, lineEnd < 0 ? value.length : lineEnd)
}

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
    } else if (event.key === 'Enter' && event.ctrlKey && !event.altKey) {
      event.preventDefault()
      const { value, selectionStart, selectionEnd } = event.currentTarget
      sendTextToActivePane(noteTextToSend(value, selectionStart, selectionEnd), event.shiftKey)
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
        placeholder="Notes de ce workspace : tâches, ports, commandes… Enregistrées avec la session. Ctrl + Entrée colle la ligne (ou la sélection) dans le terminal actif, Ctrl + Maj + Entrée la colle et l’exécute."
        className="min-h-0 flex-1 resize-none rounded border border-dock-line bg-dock-panel px-[10px] py-[8px] font-mono text-[12px] leading-[1.5] text-dock-ink outline-none placeholder:text-dock-muted focus:border-dock-focus"
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        aria-describedby={NOTE_HINT_ID}
      />
      <p id={NOTE_HINT_ID} className="mt-[6px] shrink-0 text-[11px] leading-[1.4] text-dock-muted">
        Ctrl + Entrée : coller dans le terminal · Ctrl + Maj + Entrée : exécuter
      </p>
    </section>
  )
}
