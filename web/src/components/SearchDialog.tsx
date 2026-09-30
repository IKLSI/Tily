import { useEffect, useMemo, useRef, useState, type KeyboardEvent, type PointerEvent } from 'react'
import { filterSearchItems, type SearchItem } from '../palette/searchFilter'
import { keepTabInside } from './focusTrap'

interface SearchDialogProps<T extends SearchItem> {
  label: string
  placeholder: string
  emptyMessage: string
  items: T[]
  onClose: () => void
  onRun: (item: T) => void
  onRunAlternate?: (item: T) => void
  onRunControl?: (item: T) => void
  footer?: string
  onToggleFavorite?: (item: T) => void
  notice?: string | null
  maxResults?: number
}

const RESULT_ID_PREFIX = 'search-result-'
const LISTBOX_ID = 'search-results'

export function SearchDialog<T extends SearchItem>({ label, placeholder, emptyMessage, items, onClose, onRun, onRunAlternate, onRunControl, footer, onToggleFavorite, notice, maxResults }: SearchDialogProps<T>) {
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const matching = useMemo(() => filterSearchItems(items, query), [items, query])
  const filtered = useMemo(() => (maxResults === undefined ? matching : matching.slice(0, maxResults)), [matching, maxResults])
  const hidden = matching.length - filtered.length
  const selected = Math.max(0, filtered.findIndex((item) => item.id === selectedId))
  const selectedItem = filtered[selected]

  useEffect(() => {
    listRef.current?.querySelector<HTMLElement>(`#${RESULT_ID_PREFIX}${selected}`)?.scrollIntoView({ block: 'nearest' })
  }, [selected, filtered])

  const move = (offset: number) => {
    if (filtered.length > 0) {
      setSelectedId(filtered[(selected + offset + filtered.length) % filtered.length].id)
    }
  }
  const handleQueryChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(event.target.value)
    setSelectedId(null)
  }
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.nativeEvent.isComposing) {
      return
    }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      move(event.key === 'ArrowDown' ? 1 : -1)
    } else if (event.key === 'Enter' && event.ctrlKey) {
      event.preventDefault()
      if (selectedItem && selectedItem.favorite !== undefined) {
        onToggleFavorite?.(selectedItem)
      } else if (selectedItem) {
        onRunControl?.(selectedItem)
      }
    } else if (event.key === 'Enter') {
      event.preventDefault()
      if (selectedItem) {
        runItem(selectedItem, event.shiftKey)
      }
    } else if (event.key === 'Escape') {
      event.preventDefault()
      onClose()
    }
  }
  const runItem = (item: T, alternate: boolean) => (alternate && onRunAlternate ? onRunAlternate(item) : onRun(item))
  const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onClose()
    }
  }
  const handleResultPointerDown = (event: PointerEvent<HTMLButtonElement>) => event.preventDefault()

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-dock-paper/60 pt-[12vh]" onPointerDown={handleBackdropPointerDown}>
      <div role="dialog" aria-label={label} className="flex max-h-[70vh] w-[560px] max-w-[92vw] flex-col rounded-lg border border-dock-line bg-dock-panel p-2 shadow-xl" onKeyDown={keepTabInside}>
        <input
          type="text"
          role="combobox"
          aria-autocomplete="list"
          aria-expanded="true"
          aria-controls={LISTBOX_ID}
          aria-activedescendant={selectedItem ? `${RESULT_ID_PREFIX}${selected}` : undefined}
          autoFocus
          placeholder={placeholder}
          value={query}
          className="rounded border border-dock-focus bg-dock-paper px-3 py-2 text-[13px] text-dock-ink outline-none placeholder:text-dock-muted"
          onChange={handleQueryChange}
          onKeyDown={handleKeyDown}
        />
        <div ref={listRef} id={LISTBOX_ID} role="listbox" aria-label="Résultats" className="mt-2 min-h-0 flex-1 overflow-y-auto">
          {filtered.length === 0 && <p className="px-3 py-2 text-xs text-dock-muted">{emptyMessage}</p>}
          {filtered.map((item, index) => {
            const handleHover = () => setSelectedId(item.id)
            const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => (event.ctrlKey && onRunControl && item.favorite === undefined ? onRunControl(item) : runItem(item, event.shiftKey))
            const handleToggleFavorite = (event: React.MouseEvent<HTMLButtonElement>) => {
              event.stopPropagation()
              onToggleFavorite?.(item)
            }
            const isSelected = index === selected
            return (
              <div
                key={item.id}
                id={`${RESULT_ID_PREFIX}${index}`}
                role="option"
                aria-selected={isSelected}
                className={`flex w-full items-baseline gap-2 rounded pl-3 pr-1 text-[13px] ${isSelected ? 'bg-dock-green-soft text-dock-green-deep' : 'text-dock-ink'}`}
                onPointerMove={handleHover}
              >
                <button type="button" tabIndex={-1} className="flex min-w-0 flex-1 cursor-pointer items-baseline gap-3 py-2 text-left focus:outline-none" onPointerDown={handleResultPointerDown} onClick={handleClick}>
                  <span className="min-w-0 truncate">{item.label}</span>
                  {item.hint && <span className="min-w-0 flex-1 truncate text-right font-mono text-[11px] text-dock-muted">{item.hint}</span>}
                </button>
                {item.favorite !== undefined && (
                  <button
                    type="button"
                    tabIndex={-1}
                    aria-pressed={item.favorite}
                    aria-label={item.favorite ? 'Retirer des favoris' : 'Ajouter aux favoris'}
                    data-tip={item.favorite ? 'Retirer des favoris (Ctrl + Entrée)' : 'Ajouter aux favoris (Ctrl + Entrée)'}
                    className={`shrink-0 cursor-pointer rounded px-1.5 py-1 text-[13px] leading-none hover:bg-dock-green-hover focus:outline-none ${item.favorite ? 'text-dock-warning' : 'text-dock-muted opacity-50 hover:opacity-100'}`}
                    onPointerDown={handleResultPointerDown}
                    onClick={handleToggleFavorite}
                  >
                    {item.favorite ? '★' : '☆'}
                  </button>
                )}
              </div>
            )
          })}
          {hidden > 0 && <p className="px-3 py-2 text-xs text-dock-muted">{hidden === 1 ? '1 autre résultat' : `${hidden} autres résultats`} : précisez la recherche.</p>}
        </div>
        {footer && <p className="mt-2 border-t border-dock-line px-3 pt-2 text-[11px] text-dock-muted">{footer}</p>}
        <p role="status" className={notice ? 'mt-2 border-t border-dock-line px-3 pt-2 text-xs text-dock-warning' : undefined}>
          {notice}
        </p>
      </div>
    </div>
  )
}
