import { useEffect, useRef, useState } from 'react'
import { answerConsent, ConsentAnswer } from '../mcpConsent'
import { useMcpConsentStore, type McpConsentRequest } from '../mcpConsentStore'
import { keepTabInside } from '../../../components/focusTrap'
import { BUTTON_PRIMARY, BUTTON_SECONDARY } from '../../../components/buttonStyles'

const ARM_DELAY_MS = 600
const LABEL = 'text-tily-xs text-tily-muted'
const VALUE = 'text-tily-sm text-tily-ink'
const TITLE = 'Claude Code demande votre accord'

const waitingLabel = (waiting: number): string => (waiting === 1 ? 'Une autre demande attend après celle-ci.' : `${waiting} autres demandes attendent après celle-ci.`)

function McpConsentContent({ request, waiting }: { request: McpConsentRequest; waiting: number }) {
  const refuseRef = useRef<HTMLButtonElement>(null)
  const [armed, setArmed] = useState(false)

  useEffect(() => {
    const previous = document.activeElement instanceof HTMLElement ? document.activeElement : null
    refuseRef.current?.focus()
    const timer = setTimeout(() => setArmed(true), ARM_DELAY_MS)
    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        answerConsent(request.id, ConsentAnswer.Refused)
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => {
      clearTimeout(timer)
      document.removeEventListener('keydown', handleDocumentKeyDown)
      if (previous?.isConnected && !useMcpConsentStore.getState().queue.length) {
        previous.focus()
      }
    }
  }, [request.id])

  const handleRefuse = () => answerConsent(request.id, ConsentAnswer.Refused)
  const handleAllow = () => {
    if (armed) {
      answerConsent(request.id, ConsentAnswer.Allowed)
    }
  }

  return (
    <div className="absolute inset-0 z-40 flex items-start justify-center bg-black/30 backdrop-blur-[3px] pt-[12vh]">
      <div role="alertdialog" aria-label={TITLE} className="flex max-h-[76vh] w-[560px] max-w-[94vw] flex-col rounded-tily-lg border border-tily-line bg-tily-raised shadow-tily-dialog" onKeyDown={keepTabInside}>
        <div className="flex items-baseline justify-between border-b border-tily-line px-4 py-3">
          <h2 className="text-tily-lg font-semibold text-tily-ink">{TITLE}</h2>
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4 py-4">
          <dl className="grid grid-cols-[auto_1fr] items-baseline gap-x-3 gap-y-2">
            <dt className={LABEL}>Agent</dt>
            <dd className={VALUE}>{request.agent}</dd>
            <dt className={LABEL}>Action</dt>
            <dd className={`${VALUE} font-semibold`}>{request.action}</dd>
            <dt className={LABEL}>Cible</dt>
            <dd className={VALUE}>{request.target}</dd>
          </dl>
          {request.detail && <pre className="overflow-x-auto rounded-tily-sm border border-tily-line bg-tily-terminal px-3 py-2 font-mono text-tily-sm whitespace-pre-wrap text-tily-terminal-ink">{request.detail}</pre>}
          <p className="text-tily-xs text-tily-muted">Sans réponse dans les 2 minutes, la demande est refusée.{waiting > 0 ? ` ${waitingLabel(waiting)}` : ''}</p>
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-tily-line px-4 py-3">
          <button ref={refuseRef} type="button" className={BUTTON_SECONDARY} data-tip="Entrée ou Échap" onClick={handleRefuse}>
            Refuser
          </button>
          <button type="button" className={BUTTON_PRIMARY} aria-disabled={!armed} onClick={handleAllow}>
            Autoriser
          </button>
        </div>
      </div>
    </div>
  )
}

export function McpConsentDialog() {
  const queue = useMcpConsentStore((state) => state.queue)
  const request = queue[0]
  return request ? <McpConsentContent key={request.id} request={request} waiting={queue.length - 1} /> : null
}
