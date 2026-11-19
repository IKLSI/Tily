import { memo } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { busyLabel } from '../features/git/gitBusy'
import { useGitStore } from '../features/git/gitStore'
import { StatusLevel, useHostStore } from '../stores/hostStore'
import { useWorktreeStore } from '../features/worktrees/worktreeStore'
import { formatCommandDuration } from '../features/terminal/commandNotices'
import { Spinner } from '../components/Spinner'

const STATUS_CLASSES: Record<StatusLevel, string> = {
  [StatusLevel.Info]: 'text-tily-muted',
  [StatusLevel.Warning]: 'text-tily-warning',
  [StatusLevel.Error]: 'text-tily-error',
}

export const StatusBar = memo(function StatusBar() {
  const status = useHostStore((state) => state.status)
  const unsaved = useHostStore((state) => state.unsaved)
  const { busy, busyRefs } = useGitStore(useShallow((state) => ({ busy: state.busy, busyRefs: state.busyRefs })))
  const { worktreeTasks, purge } = useWorktreeStore(useShallow((state) => ({ worktreeTasks: state.tasks.length, purge: state.purge })))
  const worktreeBusy = worktreeTasks > 0
  const waiting = worktreeTasks - 1
  const spinning = Boolean(busy) || worktreeBusy

  return (
    <footer className={`flex h-[24px] shrink-0 items-center border-t border-tily-line bg-tily-paper text-tily-xs ${spinning ? 'text-tily-ink-soft' : STATUS_CLASSES[status.level]}`}>
        <span className="flex h-full min-w-0 flex-1 items-center gap-[6px] px-3">
          {spinning && <Spinner size={10} className="shrink-0 text-tily-green" />}
          <span className="truncate py-1 [text-box:trim-both_cap_alphabetic]">{busy ? busyLabel(busy, busyRefs) : status.text}</span>
        </span>
        <span role="status" className="sr-only">
          {status.text}
        </span>
        {waiting > 0 && (
          <span className="shrink-0 px-3 text-tily-muted" data-tip={`${waiting} opération${waiting > 1 ? 's' : ''} de worktree en attente`}>
            {`+${waiting} en attente`}
          </span>
        )}
        {purge && (
          <span className="flex shrink-0 items-center gap-[6px] px-3 text-tily-muted tabular-nums" data-tip="Fichiers d’un worktree supprimé, effacés en arrière-plan">
            <Spinner size={10} className="shrink-0 text-tily-green" />
            {`Effacement de « ${purge.name} » : ${purge.files.toLocaleString('fr-FR')} fichiers (${formatCommandDuration(purge.elapsedMs)})`}
          </span>
        )}
        {unsaved && (
          <span className="shrink-0 px-3 text-tily-error" data-tip="La dernière sauvegarde a échoué : la session restera en l’état d’avant tant qu’une écriture ne réussit pas.">
            Non enregistré
          </span>
        )}
      </footer>
  )
})
