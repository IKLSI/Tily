import { AgentState, AttentionKind } from '../../bridge/messages'
import { bridge } from '../../bridge/bridge'
import { agentKey, useAgentStore } from './agentStore'
import { useSessionStore } from '../../stores/sessionStore'
import { attentionNotice, panesInState, waitingPanes, type WaitingPane } from './agentSummary'

const seen: Record<string, string> = {}
let finished = new Set<string>()

export const startAttentionNotifier = (): (() => void) =>
  useAgentStore.subscribe((state) => {
    const { session } = useSessionStore.getState()
    if (!session) {
      return
    }
    const fresh = waitingPanes(session, state.agents).filter((pane) => {
      const key = agentKey(pane.agent)
      const isNew = seen[pane.paneId] !== key && state.acknowledged[pane.paneId] !== key
      seen[pane.paneId] = key
      return isNew
    })
    const done = panesInState(session, state.agents, AgentState.Done)
    const completed = done.filter((pane) => !pane.agent.interrupted)
    const justFinished = completed.filter((pane) => !finished.has(pane.paneId))
    finished = new Set(completed.map((pane) => pane.paneId))
    const raise = (pane: WaitingPane, kind: AttentionKind) => bridge.send({ type: 'attention.raise', pane: pane.paneId, kind, ...attentionNotice(pane) })
    fresh.forEach((pane) => raise(pane, AttentionKind.Waiting))
    justFinished.forEach((pane) => raise(pane, AttentionKind.Done))
  })
