import { useShallow } from 'zustand/react/shallow'
import { busyLabel } from '../git/gitBusy'
import { useGitStore } from '../store/gitStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useWorktreeStore } from '../store/worktreeStore'
import { Spinner } from './Spinner'

const STATUS_CLASSES: Record<StatusLevel, string> = {
  [StatusLevel.Info]: 'text-dock-muted',
  [StatusLevel.Warning]: 'text-dock-warning',
  [StatusLevel.Error]: 'text-dock-error',
}

export function StatusBar() {
  const status = useHostStore((state) => state.status)
  const unsaved = useHostStore((state) => state.unsaved)
  const { busy, busyRefs } = useGitStore(useShallow((state) => ({ busy: state.busy, busyRefs: state.busyRefs })))
  const worktreeBusy = useWorktreeStore((state) => state.busy !== null)
  const spinning = Boolean(busy) || worktreeBusy

  return (
    <footer className={`flex h-[24px] shrink-0 items-center border-t border-dock-line bg-dock-paper px-3 font-mono text-[11px] ${spinning ? 'text-dock-ink-soft' : STATUS_CLASSES[status.level]}`}>
      {spinning ? (
        <span className="flex min-w-0 items-center gap-[6px]">
          <Spinner size={10} className="text-dock-green" />
          <span className="truncate py-1 [text-box:trim-both_cap_alphabetic]">{busy ? busyLabel(busy, busyRefs) : status.text}</span>
        </span>
      ) : (
        <span className="truncate">{status.text}</span>
      )}
      {unsaved && (
        <span className="ml-auto shrink-0 pl-3 text-dock-error" data-tip="La dernière sauvegarde a échoué : la session restera en l’état d’avant tant qu’une écriture ne réussit pas.">
          Non enregistré
        </span>
      )}
    </footer>
  )
}
