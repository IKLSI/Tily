import { bridge } from '../../bridge/bridge'
import type { PaneActivity } from '../../bridge/messages'

const ANSWER_TIMEOUT_MS = 3000

const waiting = new Map<number, (panes: PaneActivity[]) => void>()
let nextRequest = 1

export const queryActivity = (paneIds: string[]): Promise<PaneActivity[]> => {
  if (paneIds.length === 0 || !bridge.available) {
    return Promise.resolve([])
  }
  const request = nextRequest++
  return new Promise((resolve) => {
    const timer = setTimeout(() => {
      waiting.delete(request)
      resolve([])
    }, ANSWER_TIMEOUT_MS)
    waiting.set(request, (panes) => {
      clearTimeout(timer)
      resolve(panes)
    })
    bridge.send({ type: 'terminal.activity', panes: paneIds, request })
  })
}

export const receiveQueriedActivity = (request: number | undefined, panes: PaneActivity[]): boolean => {
  const answer = request ? waiting.get(request) : undefined
  if (!request || !answer) {
    return false
  }
  waiting.delete(request)
  answer(panes)
  return true
}
