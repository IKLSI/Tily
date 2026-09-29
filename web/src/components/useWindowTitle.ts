import { useEffect } from 'react'
import { bridge } from '../bridge/bridge'
import type { Tab, Workspace } from '../model/session'

const TITLE_SEPARATOR = ' › '
const NO_CONTEXT = ''

export const useWindowTitle = (workspace: Workspace | undefined, tab: Tab | undefined): void => {
  const context = workspace && tab ? `${workspace.name}${TITLE_SEPARATOR}${tab.name}` : NO_CONTEXT
  useEffect(() => {
    bridge.send({ type: 'window.title', title: context })
  }, [context])
}
