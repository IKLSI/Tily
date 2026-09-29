import type { KeyboardEvent } from 'react'
import { DEFAULT_GIT_GRAPH, GIT_REFERENCES_MAX, GIT_REFERENCES_MIN, RightPanelView, type GitGraphLayout } from '../model/session'
import { hideGitGraph, togglePanelView } from '../panel/rightPanel'
import { useGitStore } from '../store/gitStore'
import { useSessionStore } from '../store/sessionStore'
import { GitDragGhost } from './GitDragGhost'
import { GitGraphTable } from './GitGraphTable'
import { GitGraphToolbar } from './GitGraphToolbar'
import { GitRefsSidebar } from './GitRefsSidebar'
import { SidebarResizer } from './SidebarResizer'

interface GitGraphViewProps {
  layout: GitGraphLayout
}

const handleKeyDown = (event: KeyboardEvent<HTMLElement>) => {
  const key = event.key.toLowerCase()
  if (event.ctrlKey && event.shiftKey && key === 'g') {
    togglePanelView(RightPanelView.Git, true)
  } else if (event.ctrlKey && event.shiftKey && key === 'e') {
    togglePanelView(RightPanelView.Files, true)
  } else if (event.key === 'Escape') {
    hideGitGraph()
  } else {
    return
  }
  event.preventDefault()
  event.stopPropagation()
}

const handleResizeReferences = (referencesWidth: number) => useSessionStore.getState().setGitGraphLayout({ referencesWidth })

export function GitGraphView({ layout }: GitGraphViewProps) {
  const state = useGitStore((store) => store.state)
  const busy = useGitStore((store) => store.busy)
  if (!state) {
    return null
  }

  return (
    <section aria-label="Graphe Git" data-git-graph="" className="absolute inset-0 z-10 flex flex-col bg-dock-panel" onKeyDown={handleKeyDown}>
      {busy && <div aria-hidden="true" className="absolute inset-x-0 top-0 z-10 h-[2px] animate-pulse bg-dock-green" />}
      <GitGraphToolbar state={state} layout={layout} />
      <div className="flex min-h-0 flex-1">
        {layout.referencesOpen && (
          <>
            <GitRefsSidebar state={state} width={layout.referencesWidth} />
            <SidebarResizer width={layout.referencesWidth} min={GIT_REFERENCES_MIN} max={GIT_REFERENCES_MAX} defaultWidth={DEFAULT_GIT_GRAPH.referencesWidth} label="Largeur de la colonne des branches" onResize={handleResizeReferences} />
          </>
        )}
        <GitGraphTable state={state} layout={layout} />
      </div>
      <GitDragGhost />
    </section>
  )
}
