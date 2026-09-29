import { bridge } from '../bridge/bridge'
import { agentKey, useAgentStore } from '../store/agentStore'
import { useHostStore } from '../store/hostStore'
import { useSessionStore } from '../store/sessionStore'
import { attentionNotice, waitingPanes } from './agentSummary'

const seen: Record<string, string> = {}

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
    const { contexts } = useHostStore.getState()
    for (const pane of fresh) {
      const notice = attentionNotice(pane, state.agents[pane.paneId], contexts[pane.paneId]?.branch)
      bridge.send({ type: 'attention.raise', pane: pane.paneId, ...notice })
    }
  })
