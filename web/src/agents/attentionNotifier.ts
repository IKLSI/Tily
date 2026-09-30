import { AgentState, AttentionKind } from '../bridge/messages'
import { bridge } from '../bridge/bridge'
import { agentKey, useAgentStore } from '../store/agentStore'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
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
      const key = agentKey(state.agents[pane.paneId])
      const isNew = seen[pane.paneId] !== key && state.acknowledged[pane.paneId] !== key
      seen[pane.paneId] = key
      return isNew
    })
    const done = panesInState(session, state.agents, AgentState.Done)
    const justFinished = done.filter((pane) => !finished.has(pane.paneId) && !state.agents[pane.paneId].interrupted)
    finished = new Set(done.map((pane) => pane.paneId))
    const { contexts } = useHostStore.getState()
    const raise = (pane: WaitingPane, kind: AttentionKind) => {
      const notice = attentionNotice(pane, state.agents[pane.paneId], contexts[pane.paneId]?.branch)
      bridge.send({ type: 'attention.raise', pane: pane.paneId, kind, ...notice })
    }
    fresh.forEach((pane) => raise(pane, AttentionKind.Waiting))
    justFinished.forEach((pane) => raise(pane, AttentionKind.Done))
  })
