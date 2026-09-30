import { bridge } from '../bridge/bridge'
import { allPanes, DEFAULT_SHELL } from '../model/session'
import { usePaneStore } from '../store/paneStore'
import { useSessionStore } from '../store/sessionStore'

export const TREE_PATH_TYPE = 'application/x-tily-path'
const FILES_TYPE = 'Files'
const EXTERNAL_TYPES = [FILES_TYPE, 'text/uri-list']
const NO_DROP = 'none'
const COPY_DROP = 'copy'
const PANE_SELECTOR = '[data-pane-id]'

const TREE_DRAG_PREFIX = `${Array.from(crypto.getRandomValues(new Uint32Array(4)), (part) => part.toString(16)).join('')}|`

export const treeDragValue = (path: string): string => `${TREE_DRAG_PREFIX}${path}`

const carries = (event: DragEvent, type: string): boolean => Boolean(event.dataTransfer?.types.includes(type))

const carriesExternalData = (event: DragEvent): boolean => EXTERNAL_TYPES.some((type) => carries(event, type))

const carriesDroppableData = (event: DragEvent): boolean => carriesExternalData(event) || carries(event, TREE_PATH_TYPE)

const carriesPaths = (event: DragEvent): boolean => carries(event, FILES_TYPE) || carries(event, TREE_PATH_TYPE)

const treePathOf = (event: DragEvent): string | null => {
  const value = event.dataTransfer?.getData(TREE_PATH_TYPE) ?? ''
  return value.startsWith(TREE_DRAG_PREFIX) ? value.slice(TREE_DRAG_PREFIX.length) : null
}

const acceptsInput = (paneId: string): boolean => !usePaneStore.getState().states[paneId]

const openPaneIdUnder = (event: DragEvent): string | undefined => {
  const paneId = event.target instanceof Element ? event.target.closest<HTMLElement>(PANE_SELECTOR)?.dataset.paneId : undefined
  return paneId && acceptsInput(paneId) ? paneId : undefined
}

const shellOf = (paneId: string): string => {
  const { session } = useSessionStore.getState()
  return (session ? allPanes(session).find((pane) => pane.id === paneId)?.shell : undefined) ?? DEFAULT_SHELL
}

export const insertPathIntoPane = (paneId: string, path: string): boolean => {
  if (!acceptsInput(paneId)) {
    return false
  }
  bridge.send({ type: 'terminal.dropPath', pane: paneId, shell: shellOf(paneId), path })
  return true
}

const handleDragOver = (event: DragEvent): void => {
  if (!carriesDroppableData(event)) {
    return
  }
  event.preventDefault()
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = carriesPaths(event) && openPaneIdUnder(event) ? COPY_DROP : NO_DROP
  }
}

const handleDrop = (event: DragEvent): void => {
  if (!carriesDroppableData(event)) {
    return
  }
  event.preventDefault()
  const paneId = openPaneIdUnder(event)
  const treePath = treePathOf(event)
  const files = event.dataTransfer?.files
  if (paneId && treePath) {
    insertPathIntoPane(paneId, treePath)
  } else if (paneId && files && files.length > 0) {
    bridge.sendWithFiles({ type: 'terminal.drop', pane: paneId, shell: shellOf(paneId) }, files)
  }
}

export const startExternalDrops = (): (() => void) => {
  window.addEventListener('dragover', handleDragOver)
  window.addEventListener('drop', handleDrop)
  return () => {
    window.removeEventListener('dragover', handleDragOver)
    window.removeEventListener('drop', handleDrop)
  }
}
