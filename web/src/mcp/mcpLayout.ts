import type { PaneAgent } from '../bridge/messages'
import { panesOf, type Session } from '../model/session'

interface McpPaneAgent {
  name: string
  state: string
  message?: string
}

interface McpPaneLayout {
  id: string
  path: string
  shell: string
  active: boolean
  started: boolean
  caller?: true
  agent?: McpPaneAgent
}

interface McpTabLayout {
  id: string
  name: string
  active: boolean
  panes: McpPaneLayout[]
}

interface McpWorkspaceLayout {
  id: string
  name: string
  active: boolean
  tabs: McpTabLayout[]
}

interface McpCaller {
  workspace: string
  tab: string
  pane: string
}

export interface McpLayout {
  caller: McpCaller | null
  workspaces: McpWorkspaceLayout[]
}

const agentOf = (agent: PaneAgent | undefined): McpPaneAgent | undefined => (agent ? { name: agent.agent, state: agent.state, message: agent.message } : undefined)

export const layoutOf = (session: Session, agents: Record<string, PaneAgent>, callerPane: string | undefined, started: (paneId: string) => boolean): McpLayout => {
  let caller: McpCaller | null = null
  const workspaces = session.workspaces.map((workspace) => ({
    id: workspace.id,
    name: workspace.name,
    active: workspace.id === session.active,
    tabs: workspace.tabs.map((tab) => ({
      id: tab.id,
      name: tab.name,
      active: tab.id === workspace.active,
      panes: panesOf(tab.tree).map((pane) => {
        const isCaller = pane.id === callerPane
        if (isCaller) {
          caller = { workspace: workspace.id, tab: tab.id, pane: pane.id }
        }
        return { id: pane.id, path: pane.path, shell: pane.shell, active: pane.id === tab.active, started: started(pane.id), caller: isCaller ? (true as const) : undefined, agent: agentOf(agents[pane.id]) }
      }),
    })),
  }))
  return { caller, workspaces }
}
