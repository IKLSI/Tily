import { useEffect, useRef, useState, type ChangeEvent, type KeyboardEvent } from 'react'
import type { ISearchOptions } from '@xterm/addon-search'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { ICON_BUTTON } from '../../../components/controlStyles'
import { useUiStore } from '../../../stores/uiStore'
import { focusPane } from '../terminalActions'
import { terminalRegistry } from '../terminalRegistry'

interface PaneSearchBarProps {
  paneId: string
}

interface SearchResults {
  index: number
  count: number
}

const NO_RESULTS: SearchResults = { index: -1, count: 0 }
const BAR_BUTTON = `${ICON_BUTTON} size-[20px]`

const HIDDEN_RULER_MARK = '#121416'

const SEARCH_OPTIONS: ISearchOptions = {
  decorations: {
    matchBackground: '#3d4f46',
    matchBorder: '#5b7f6c',
    matchOverviewRuler: HIDDEN_RULER_MARK,
    activeMatchBackground: '#5b7f6c',
    activeMatchBorder: '#8fb39f',
    activeMatchColorOverviewRuler: HIDDEN_RULER_MARK,
  },
}

const resultsLabel = (term: string, results: SearchResults): string => {
  if (term.length === 0) {
    return ''
  }
  return results.count === 0 ? 'Aucun résultat' : `${Math.max(results.index, 0) + 1} / ${results.count}`
}

export function PaneSearchBar({ paneId }: PaneSearchBarProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [term, setTerm] = useState('')
  const [results, setResults] = useState<SearchResults>(NO_RESULTS)
  const request = useUiStore((state) => state.paneSearchRequest)

  useEffect(() => {
    inputRef.current?.focus()
    inputRef.current?.select()
  }, [request])

  useEffect(() => {
    const search = terminalRegistry.get(paneId)?.search
    if (!search) {
      return
    }
    const subscription = search.onDidChangeResults(({ resultIndex, resultCount }) => setResults({ index: resultIndex, count: resultCount }))
    return () => {
      subscription.dispose()
      search.clearDecorations()
      terminalRegistry.get(paneId)?.terminal.clearSelection()
    }
  }, [paneId])

  const close = () => {
    useUiStore.getState().closePaneSearch()
    focusPane(paneId)
  }

  const findNext = () => terminalRegistry.get(paneId)?.search.findNext(term, SEARCH_OPTIONS)
  const findPrevious = () => terminalRegistry.get(paneId)?.search.findPrevious(term, SEARCH_OPTIONS)

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const next = event.target.value
    setTerm(next)
    const search = terminalRegistry.get(paneId)?.search
    if (next.length === 0) {
      search?.clearDecorations()
      terminalRegistry.get(paneId)?.terminal.clearSelection()
      setResults(NO_RESULTS)
    } else {
      search?.findNext(next, { ...SEARCH_OPTIONS, incremental: true })
    }
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Escape') {
      event.preventDefault()
      event.stopPropagation()
      close()
    } else if (event.key === 'Enter') {
      event.preventDefault()
      if (event.shiftKey) {
        findPrevious()
      } else {
        findNext()
      }
    } else if (event.metaKey && event.key.toLowerCase() === 'f') {
      event.preventDefault()
      inputRef.current?.select()
    }
  }

  return (
    <div role="search" data-pane-search className="absolute top-2 right-3 z-10 flex items-center gap-1 rounded-tily-sm border border-tily-line bg-tily-raised py-1 pr-1.5 pl-2.5 text-tily-sm shadow-lg transition-colors focus-within:border-tily-focus" onMouseDown={(event) => event.stopPropagation()}>
      <input
        ref={inputRef}
        type="text"
        value={term}
        placeholder="Rechercher"
        aria-label="Rechercher dans le terminal"
        spellCheck={false}
        autoComplete="off"
        className="w-40 bg-transparent text-tily-ink outline-none placeholder:text-tily-muted"
        onChange={handleChange}
        onKeyDown={handleKeyDown}
      />
      <span className="min-w-[4.5em] text-right text-tily-muted tabular-nums" aria-live="polite">
        {resultsLabel(term, results)}
      </span>
      <button type="button" className={BAR_BUTTON} data-tip="Résultat précédent (Maj + Entrée)" aria-label="Résultat précédent" onClick={findPrevious}>
        <Icon name={IconName.Chevron} className="-rotate-90" />
      </button>
      <button type="button" className={BAR_BUTTON} data-tip="Résultat suivant (Entrée)" aria-label="Résultat suivant" onClick={findNext}>
        <Icon name={IconName.Chevron} className="rotate-90" />
      </button>
      <button type="button" className={BAR_BUTTON} data-tip="Fermer (Échap)" aria-label="Fermer la recherche" onClick={close}>
        <Icon name={IconName.Close} />
      </button>
    </div>
  )
}
