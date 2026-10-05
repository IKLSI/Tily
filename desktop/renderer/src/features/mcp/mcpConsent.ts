import { bridge } from '../../bridge/bridge'
import { folderName } from '../../model/session'
import { StatusLevel, useHostStore } from '../../stores/hostStore'
import { useMcpConsentStore, type McpConsentRequest } from './mcpConsentStore'
import { locationOf, placeOf, requireSession, type PanePlace } from './mcpPanes'

const CONSENT_TIMEOUT_MS = 120_000

export enum ConsentAnswer {
  Allowed = 'allowed',
  Refused = 'refused',
  Expired = 'expired',
}

interface ConsentDemand {
  caller: string | undefined
  action: string
  target: string
  detail?: string
}

const answers = new Map<number, (answer: ConsentAnswer) => void>()
let nextId = 1

export const agentLabel = (caller: string | undefined): string => {
  const place = placeOf(requireSession(), caller)
  return place ? `Claude Code dans « ${locationOf(place)} »` : 'Claude Code'
}

export const paneLabel = (place: PanePlace): string =>
  `pane « ${locationOf(place)} » · ${folderName(place.pane.path) || place.pane.path} (${place.pane.owner ? 'créé par Claude' : 'ouvert par vous'})`

const sentence = (action: string): string => action.charAt(0).toLowerCase() + action.slice(1)

export const answerConsent = (id: number, answer: ConsentAnswer): void => {
  const resolve = answers.get(id)
  answers.delete(id)
  useMcpConsentStore.getState().remove(id)
  resolve?.(answer)
}

const ask = (request: Omit<McpConsentRequest, 'id'>): Promise<ConsentAnswer> =>
  new Promise((resolve) => {
    const id = nextId++
    const timer = setTimeout(() => answerConsent(id, ConsentAnswer.Expired), CONSENT_TIMEOUT_MS)
    answers.set(id, (answer) => {
      clearTimeout(timer)
      resolve(answer)
    })
    useMcpConsentStore.getState().push({ ...request, id })
    bridge.send({ type: 'attention.flash' })
    useHostStore.getState().setStatus(`${request.agent} demande votre accord : ${sentence(request.action)}.`, StatusLevel.Warning)
  })

export const requireConsent = async (demand: ConsentDemand): Promise<void> => {
  const answer = await ask({ agent: agentLabel(demand.caller), action: demand.action, target: demand.target, detail: demand.detail })
  const action = sentence(demand.action)
  if (answer === ConsentAnswer.Refused) {
    throw new Error(`L’utilisateur a refusé dans Tily : ${action} (${demand.target}). Ne réessayez pas sans le lui demander.`)
  }
  if (answer === ConsentAnswer.Expired) {
    useHostStore.getState().setStatus(`Demande de Claude Code refusée faute de réponse en 2 minutes : ${action}.`, StatusLevel.Warning)
    throw new Error(`Pas de réponse de l’utilisateur dans Tily en 2 minutes : ${action} (${demand.target}) n’a pas été fait.`)
  }
}
