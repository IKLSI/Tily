import { useEffect, type ChangeEvent, type PointerEvent, type SubmitEvent } from 'react'
import { WorktreeBranchMode, type WorktreePlan } from '../bridge/worktreeMessages'
import { folderName } from '../model/session'
import type { WorktreeDraft, WorktreeFailure } from '../store/worktreeStore'
import { changeWorktreeDraft, closeWorktreeDialog, submitWorktree } from '../worktree/worktreeActions'
import { keepTabInside } from './focusTrap'
import { SETTINGS_BUTTON, SETTINGS_HINT, SETTINGS_INPUT, SETTINGS_LABEL, SETTINGS_SECONDARY } from './settingsStyles'
import { Spinner } from './Spinner'
import { WorktreeFailureDetails } from './WorktreeFailureDetails'

interface WorktreeDialogProps {
  draft: WorktreeDraft
  plan: WorktreePlan | null
  planPending: boolean
  busy: boolean
  failure: WorktreeFailure | null
}

const PRIMARY = `${SETTINGS_BUTTON} flex items-center gap-2 border-dock-green text-dock-green-deep hover:bg-dock-green-soft disabled:cursor-default disabled:opacity-50 disabled:hover:bg-transparent`
const RADIO_LABEL = 'flex items-center gap-2 text-[12px] text-dock-ink'
const LOCAL_GROUP = 'Locales'
const REMOTE_GROUP = 'Distantes'

const handleBackdropPointerDown = (event: PointerEvent<HTMLDivElement>) => {
  if (event.target === event.currentTarget) {
    closeWorktreeDialog()
  }
}

const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
  event.preventDefault()
  submitWorktree()
}

const handleNewMode = () => changeWorktreeDraft({ mode: WorktreeBranchMode.New, branch: '' })
const handleExistingMode = () => changeWorktreeDraft({ mode: WorktreeBranchMode.Local, branch: '' })
const handleBranchChange = (event: ChangeEvent<HTMLInputElement>) => changeWorktreeDraft({ branch: event.target.value })
const handleBaseChange = (event: ChangeEvent<HTMLSelectElement>) => changeWorktreeDraft({ base: event.target.value })
const handleInstallChange = (event: ChangeEvent<HTMLInputElement>) => changeWorktreeDraft({ install: event.target.checked })
const handleDatabaseChange = (event: ChangeEvent<HTMLInputElement>) => changeWorktreeDraft({ database: event.target.checked })

const branchOptions = (label: string, branches: string[]) =>
  branches.length > 0 && (
    <optgroup label={label}>
      {branches.map((branch) => (
        <option key={branch} value={branch}>
          {branch}
        </option>
      ))}
    </optgroup>
  )

export function WorktreeDialog({ draft, plan, planPending, busy, failure }: WorktreeDialogProps) {
  const creating = draft.mode === WorktreeBranchMode.New
  const localBranches = plan?.localBranches ?? []
  const remoteBranches = plan?.remoteBranches ?? []
  const defaultBase = plan?.defaultBase ?? ''
  const baseValue = draft.base || defaultBase
  const extraBase = baseValue && !remoteBranches.includes(baseValue) && !localBranches.includes(baseValue) ? [baseValue] : []
  const canCreate = Boolean(plan?.path) && !plan?.error && !planPending && !busy

  useEffect(() => {
    const handleDocumentKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        closeWorktreeDialog()
      }
    }
    document.addEventListener('keydown', handleDocumentKeyDown)
    return () => document.removeEventListener('keydown', handleDocumentKeyDown)
  }, [])

  const handleExistingChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const branch = event.target.value
    changeWorktreeDraft({ branch, mode: remoteBranches.includes(branch) ? WorktreeBranchMode.Remote : WorktreeBranchMode.Local })
  }

  return (
    <div className="absolute inset-0 z-30 flex items-start justify-center bg-dock-paper/60 pt-[10vh]" onPointerDown={handleBackdropPointerDown}>
      <form role="dialog" aria-label="Créer un worktree" className="flex max-h-[80vh] w-[560px] max-w-[94vw] flex-col rounded-lg border border-dock-line bg-dock-panel shadow-xl" onKeyDown={keepTabInside} onSubmit={handleSubmit}>
        <div className="flex items-center justify-between border-b border-dock-line px-4 py-3">
          <h2 className="text-[15px] font-semibold text-dock-ink">Créer un worktree</h2>
          <span className={SETTINGS_HINT}>Entrée crée · Échap ferme</span>
        </div>
        <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
          <div className="flex flex-col gap-1">
            <span className={SETTINGS_LABEL}>{`Projet : ${plan?.project ?? folderName(draft.repository)}`}</span>
            <span className={`${SETTINGS_HINT} truncate font-mono`}>{plan?.repository ?? draft.repository}</span>
          </div>
          <fieldset className="flex flex-col gap-2" disabled={busy}>
            <label className={RADIO_LABEL}>
              <input type="radio" name="worktree-mode" checked={creating} onChange={handleNewMode} />
              Nouvelle branche
            </label>
            {creating && (
              <div className="flex flex-col gap-2 pl-6">
                <input type="text" className={SETTINGS_INPUT} value={draft.branch} placeholder="feat/ma-branche" spellCheck={false} autoFocus aria-label="Nom de la nouvelle branche" onChange={handleBranchChange} />
                <label className="flex items-center gap-2">
                  <span className={`${SETTINGS_LABEL} shrink-0`}>depuis</span>
                  <select className={SETTINGS_INPUT} value={baseValue} aria-label="Base de la nouvelle branche" onChange={handleBaseChange}>
                    {branchOptions('Base par défaut', extraBase)}
                    {branchOptions(REMOTE_GROUP, remoteBranches)}
                    {branchOptions(LOCAL_GROUP, localBranches)}
                  </select>
                </label>
              </div>
            )}
            <label className={RADIO_LABEL}>
              <input type="radio" name="worktree-mode" checked={!creating} onChange={handleExistingMode} />
              Branche existante
            </label>
            {!creating && (
              <div className="pl-6">
                <select className={SETTINGS_INPUT} value={draft.branch} autoFocus aria-label="Branche existante" onChange={handleExistingChange}>
                  <option value="">Choisir une branche…</option>
                  {branchOptions(LOCAL_GROUP, localBranches)}
                  {branchOptions(REMOTE_GROUP, remoteBranches)}
                </select>
              </div>
            )}
          </fieldset>
          <div className="flex flex-col gap-1">
            <span className={SETTINGS_LABEL}>Dossier</span>
            <span className={`${SETTINGS_HINT} truncate font-mono`}>{plan?.path ?? '—'}</span>
            {plan?.error && <span className="text-[11px] text-dock-error">{plan.error}</span>}
          </div>
          <fieldset className="flex flex-col gap-2" disabled={busy}>
            <label className={RADIO_LABEL}>
              <input type="checkbox" checked={draft.install} onChange={handleInstallChange} />
              Lancer pnpm install dans le terminal du worktree (si package.json existe)
            </label>
            <label className={RADIO_LABEL}>
              <input type="checkbox" checked={draft.database} onChange={handleDatabaseChange} />
              Répliquer la base de données (PostgreSQL sous Docker ou SQL Server)
            </label>
            <span className={SETTINGS_HINT}>Les ports de développement sont toujours remplacés par des ports libres.</span>
          </fieldset>
          {failure && <WorktreeFailureDetails failure={failure} />}
        </div>
        <div className="flex items-center justify-end gap-2 border-t border-dock-line px-4 py-3">
          <button type="button" className={SETTINGS_SECONDARY} onClick={closeWorktreeDialog}>
            {busy ? 'Fermer' : 'Annuler'}
          </button>
          <button type="submit" className={PRIMARY} disabled={!canCreate}>
            {busy && <Spinner size={10} className="text-dock-green" />}
            {busy ? 'Création…' : 'Créer'}
          </button>
        </div>
      </form>
    </div>
  )
}
