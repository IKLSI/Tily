import { useEffect, type ChangeEvent, type PointerEvent, type SubmitEvent } from 'react'
import type { ClonePlan } from '../../../bridge/cloneMessages'
import { keepTabInside } from '../../../components/focusTrap'
import { Spinner } from '../../../components/Spinner'
import { SETTINGS_BUTTON, SETTINGS_CHECKBOX, SETTINGS_HINT, SETTINGS_INPUT, SETTINGS_SECONDARY } from '../../settings/components/settingsStyles'
import { WorktreeFailureDetails } from '../../worktrees/components/WorktreeFailureDetails'
import { changeCloneDraft, closeCloneDialog, submitClone } from '../cloneActions'
import type { CloneDraft, CloneFailure } from '../cloneStore'
import { CloneFolderField } from './CloneFolderField'

interface CloneDialogProps {
  draft: CloneDraft
  plan: ClonePlan | null
  planPending: boolean
  roots: string[]
  busy: boolean
  progress: string | null
  failure: CloneFailure | null
}

const PRIMARY = `${SETTINGS_BUTTON} flex items-center gap-2 border-tily-green text-tily-green-deep hover:bg-tily-green-soft disabled:cursor-default disabled:opacity-50 disabled:hover:bg-transparent`
const ROW_LABEL = 'flex min-h-[28px] items-center text-tily-sm text-tily-muted'

const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
  event.preventDefault()
  submitClone(false)
}

const handleUrlChange = (event: ChangeEvent<HTMLInputElement>) => changeCloneDraft({ url: event.target.value })
const handleNameChange = (event: ChangeEvent<HTMLInputElement>) => changeCloneDraft({ name: event.target.value })
const handleInstallChange = (event: ChangeEvent<HTMLInputElement>) => changeCloneDraft({ install: event.target.checked })
const handleClaudeChange = (event: ChangeEvent<HTMLInputElement>) => changeCloneDraft({ claude: event.target.checked })

export function CloneDialog({ draft, plan, planPending, roots, busy, progress, failure }: CloneDialogProps) {
  const canClone = draft.url.trim().length > 0 && Boolean(plan?.path) && !plan?.error && !planPending && !busy
  const destination = plan?.path ?? plan?.pathPreview ?? ''
  const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget && draft.url.trim().length === 0) {
      closeCloneDialog()
    }
  }

  useEffect(() => {
    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        closeCloneDialog()
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => document.removeEventListener('keydown', handleDocumentKeyDown)
  }, [])

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-black/30 backdrop-blur-[3px] pt-[10vh]" onPointerDown={handleBackdropPointerDown}>
      <form role="dialog" aria-label="Cloner un dépôt" className="flex max-h-[80vh] w-[560px] max-w-[94vw] flex-col rounded-tily-lg border border-tily-line bg-tily-raised shadow-tily-dialog" onKeyDown={keepTabInside} onSubmit={handleSubmit}>
        <div className="flex items-center gap-2 border-b border-tily-line px-4 py-3">
          <h2 className="text-tily-lg font-semibold text-tily-ink">Cloner un dépôt</h2>
        </div>
        <div className="grid min-h-0 flex-1 grid-cols-[auto_minmax(0,1fr)] items-start gap-x-4 gap-y-4 overflow-y-auto px-4 py-4">
          <span className={ROW_LABEL}>Adresse</span>
          <fieldset className="min-w-0" disabled={busy}>
            <input type="text" className={SETTINGS_INPUT} value={draft.url} placeholder="https://github.com/organisation/depot.git" spellCheck={false} autoFocus aria-label="Adresse du dépôt" onChange={handleUrlChange} />
          </fieldset>
          <span className={ROW_LABEL}>Dossier</span>
          <fieldset className="min-w-0" disabled={busy}>
            <CloneFolderField draft={draft} plan={plan} roots={roots} />
          </fieldset>
          <span className={ROW_LABEL}>Nom</span>
          <fieldset className="flex min-w-0 flex-col gap-1.5" disabled={busy}>
            <input type="text" className={SETTINGS_INPUT} value={draft.name} placeholder={plan?.name || 'Nom du dossier'} spellCheck={false} aria-label="Nom du dossier cloné" onChange={handleNameChange} />
            {destination && <span className={`${SETTINGS_HINT} truncate`} data-tip={destination}>{destination}</span>}
            {plan?.error && <span className="text-tily-xs text-tily-error">{plan.error}</span>}
          </fieldset>
          <span className={ROW_LABEL}>Options</span>
          <fieldset className="min-w-0" disabled={busy}>
            <div className="flex min-h-[28px] flex-wrap items-center gap-x-4 gap-y-1">
              <label className={SETTINGS_CHECKBOX}>
                <input type="checkbox" checked={draft.claude} onChange={handleClaudeChange} />
                Claude Code
              </label>
              <label className={SETTINGS_CHECKBOX}>
                <input type="checkbox" checked={draft.install} onChange={handleInstallChange} />
                pnpm install
              </label>
            </div>
          </fieldset>
          {busy && (
            <div className="col-span-2 flex items-center gap-2 text-tily-sm text-tily-muted" role="status">
              <Spinner size={10} className="text-tily-green" />
              <span className="truncate">{progress ?? 'Clonage en cours…'}</span>
            </div>
          )}
          {failure && (
            <div className="col-span-2">
              <WorktreeFailureDetails failure={failure} />
            </div>
          )}
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-tily-line px-4 py-3">
          <button type="button" className={SETTINGS_SECONDARY} onClick={closeCloneDialog}>
            {busy ? 'Fermer' : 'Annuler'}
          </button>
          <button type="submit" className={PRIMARY} disabled={!canClone}>
            {busy && <Spinner size={10} className="text-tily-green" />}
            {busy ? 'Clonage…' : 'Cloner'}
          </button>
        </div>
      </form>
    </div>
  )
}
