import { bridge } from '../bridge/bridge'
import { McpTool } from '../bridge/mcpMessages'
import type { HostMessageOf } from '../bridge/messages'
import { useAgentStore } from '../store/agentStore'
import { useSessionStore } from '../store/sessionStore'
import { terminalRegistry } from '../terminal/terminalRegistry'
import { layoutOf } from './mcpLayout'

type McpRequest = HostMessageOf<'mcp.request'>

const requireSession = () => {
  const { session } = useSessionStore.getState()
  if (!session) {
    throw new Error('La session de Tily n’est pas encore chargée.')
  }
  return session
}

const answer = async (request: McpRequest): Promise<unknown> => {
  switch (request.tool) {
    case McpTool.Layout:
      return layoutOf(requireSession(), useAgentStore.getState().agents, request.pane, (paneId) => terminalRegistry.get(paneId)?.started ?? false)
    default:
      throw new Error(`Outil Tily inconnu : ${request.tool}. Mettez Tily à jour.`)
  }
}

export const receiveMcpRequest = async (request: McpRequest): Promise<void> => {
  try {
    bridge.send({ type: 'mcp.response', id: request.id, result: await answer(request) })
  } catch (error) {
    bridge.send({ type: 'mcp.response', id: request.id, error: error instanceof Error ? error.message : String(error) })
  }
}

export const installMcpServer = (): void => bridge.send({ type: 'mcp.install' })

export const removeMcpServer = (): void => bridge.send({ type: 'mcp.remove' })
