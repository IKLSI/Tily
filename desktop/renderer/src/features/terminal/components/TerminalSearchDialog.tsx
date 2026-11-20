import { useEffect, useMemo, useRef, useState, type ChangeEvent, type KeyboardEvent, type PointerEvent } from 'react'
import { keepTabInside } from '../../../components/focusTrap'
import { SearchOption, searchPattern } from '../../../model/textSearch'
import { closeTerminalSearch, collectTerminalLines, goToTerminalLine } from '../globalSearch'
import { findTerminalLines, matchPartsOf } from '../terminalLines'
import { useTerminalSearchStore } from '../terminalSearchStore'

const MAX_RESULTS = 200
const RESULT_ID_PREFIX = 'terminal-search-result-'

const OPTION_BUTTONS = [
  { option: SearchOption.CaseSensitive, code: 'KeyC', label: 'Aa', tip: 'Respecter la casse' },
  { option: SearchOption.WholeWord, code: 'KeyW', label: 'ab', tip: 'Mot entier' },
  { option: SearchOption.Regex, code: 'KeyR', label: '.*', tip: 'Expression régulière' },
]

const summaryOf = (query: string, valid: boolean, total: number, panes: number): string => {
  if (query === '') {
    return ''
  }
  if (!valid) {
    return 'Expression invalide'
  }
  if (total === 0) {
    return 'Aucun résultat'
  }
  const lines = total === 1 ? '1 ligne' : `${total} lignes`
  const paneLabel = panes === 1 ? '1 pane' : `${panes} panes`
  return `${lines} dans ${paneLabel}`
}

export function TerminalSearchDialog() {
  const open = useTerminalSearchStore((state) => state.open)
  return open ? <OpenTerminalSearchDialog /> : null
}

function OpenTerminalSearchDialog() {
  const query = useTerminalSearchStore((state) => state.query)
  const options = useTerminalSearchStore((state) => state.options)
  const [selected, setSelected] = useState(0)
  const listRef = useRef<HTMLDivElement>(null)
  const lines = useMemo(() => collectTerminalLines(), [])
  const results = useMemo(() => findTerminalLines(lines, query, options, MAX_RESULTS), [lines, query, options])
  const valid = query === '' || searchPattern(query, options) !== null
  const current = Math.min(selected, Math.max(0, results.matches.length - 1))
  const hidden = results.total - results.matches.length

  useEffect(() => {
    listRef.current?.querySelector<HTMLElement>(`#${RESULT_ID_PREFIX}${current}`)?.scrollIntoView({ block: 'nearest' })
  }, [current, results])

  const move = (offset: number) => {
    if (results.matches.length > 0) {
      setSelected((current + offset + results.matches.length) % results.matches.length)
    }
  }
  const handleQueryChange = (event: ChangeEvent<HTMLInputElement>) => {
    useTerminalSearchStore.getState().setQuery(event.target.value)
    setSelected(0)
  }
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.nativeEvent.isComposing) {
      return
    }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      move(event.key === 'ArrowDown' ? 1 : -1)
    } else if (event.key === 'Enter') {
      event.preventDefault()
      const line = results.matches[current]
      if (line) {
        goToTerminalLine(line)
      }
    } else if (event.key === 'Escape') {
      event.preventDefault()
      closeTerminalSearch()
    } else if (event.altKey && !event.ctrlKey && !event.metaKey) {
      const entry = OPTION_BUTTONS.find(({ code }) => code === event.code)
      if (entry) {
        event.preventDefault()
        useTerminalSearchStore.getState().toggleOption(entry.option)
      }
    }
  }
  const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      closeTerminalSearch()
    }
  }
  const handleResultPointerDown = (event: PointerEvent<HTMLButtonElement>) => event.preventDefault()

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-black/30 backdrop-blur-[3px] pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="dialog" aria-label="Rechercher dans tous les terminaux" className="flex max-h-[70vh] w-[680px] max-w-[92vw] flex-col rounded-tily-lg border border-tily-line bg-tily-raised p-2 shadow-tily-dialog" onKeyDown={keepTabInside}>
        <div className="flex items-center gap-1">
          <input
            type="text"
            autoFocus
            spellCheck={false}
            autoComplete="off"
            placeholder="Rechercher dans tous les terminaux"
            aria-label="Rechercher dans tous les terminaux"
            value={query}
            className={`min-w-0 flex-1 rounded-tily-sm border bg-tily-paper px-3 py-2 text-tily-base text-tily-ink outline-none placeholder:text-tily-muted ${valid ? 'border-tily-focus' : 'border-tily-warning'}`}
            onChange={handleQueryChange}
            onKeyDown={handleKeyDown}
          />
          {OPTION_BUTTONS.map(({ option, label, tip }) => (
            <button
              key={option}
              type="button"
              tabIndex={-1}
              aria-pressed={options[option]}
              data-tip={tip}
              aria-label={tip}
              className={`h-8 min-w-8 cursor-pointer rounded-tily-sm px-1.5 text-tily-sm font-mono ${options[option] ? 'bg-tily-green-soft text-tily-green-deep' : 'text-tily-muted hover:bg-tily-green-hover'}`}
              onPointerDown={(event) => event.preventDefault()}
              onClick={() => useTerminalSearchStore.getState().toggleOption(option)}
            >
              {label}
            </button>
          ))}
        </div>
        <div ref={listRef} role="listbox" aria-label="Résultats" className="mt-2 min-h-0 flex-1 overflow-y-auto">
          {query !== '' && valid && results.matches.length === 0 && <p className="px-3 py-2 text-tily-xs text-tily-muted">Aucune ligne des terminaux ne correspond à la recherche.</p>}
          {results.matches.map((line, index) => {
            const { before, matched, after } = matchPartsOf(line)
            const isSelected = index === current
            return (
              <div key={line.id} id={`${RESULT_ID_PREFIX}${index}`} role="option" aria-selected={isSelected} className={`rounded-tily-sm ${isSelected ? 'bg-tily-green-soft text-tily-green-deep' : 'text-tily-ink'}`} onPointerMove={() => setSelected(index)}>
                <button type="button" tabIndex={-1} className="flex w-full cursor-pointer flex-col gap-0.5 px-3 py-1.5 text-left focus:outline-none" onPointerDown={handleResultPointerDown} onClick={() => goToTerminalLine(line)}>
                  <span className="truncate font-mono text-tily-sm whitespace-pre">
                    {before}
                    <mark className="rounded-sm bg-tily-warning/30 text-inherit">{matched}</mark>
                    {after}
                  </span>
                  <span className="truncate text-tily-xs text-tily-muted">{line.location}</span>
                </button>
              </div>
            )
          })}
          {hidden > 0 && <p className="px-3 py-2 text-tily-xs text-tily-muted">{hidden === 1 ? '1 autre résultat' : `${hidden} autres résultats`} : précisez la recherche.</p>}
        </div>
        <p role="status" className="mt-2 border-t border-tily-line px-3 pt-2 text-tily-xs text-tily-muted">
          {summaryOf(query, valid, results.total, results.panes) || 'Entrée : aller à la ligne · Alt+C : casse · Alt+W : mot entier · Alt+R : expression régulière'}
        </p>
      </div>
    </div>
  )
}
