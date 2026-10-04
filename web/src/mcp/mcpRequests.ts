import { bridge } from '../bridge/bridge'
import { McpTool } from '../bridge/mcpMessages'
import type { HostMessageOf } from '../bridge/messages'
import { useAgentStore } from '../store/agentStore'
import { terminalRegistry } from '../terminal/terminalRegistry'
import { argumentsOf } from './mcpArguments'
import { listCommands } from './mcpCommands'
import { layoutOf } from './mcpLayout'
import { requireSession } from './mcpPanes'
import { readPane } from './mcpReadPane'
import { waitFor } from './mcpWaitFor'

type McpRequest = HostMessageOf<'mcp.request'>

const answer = async (request: McpRequest): Promise<unknown> => {
  switch (request.tool) {
    case McpTool.Layout:
      return layoutOf(requireSession(), useAgentStore.getState().agents, request.pane, (paneId) => terminalRegistry.get(paneId)?.started ?? false)
    case McpTool.ReadPane:
      return readPane(argumentsOf(request.arguments))
    case McpTool.Commands:
      return listCommands(argumentsOf(request.arguments))
    case McpTool.WaitFor:
      return waitFor(argumentsOf(request.arguments))
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
