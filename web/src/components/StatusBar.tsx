import { useShallow } from 'zustand/react/shallow'
import { busyLabel } from '../git/gitBusy'
import { useGitStore } from '../store/gitStore'
import { StatusLevel, useHostStore } from '../store/hostStore'
import { useStatusLogStore } from '../store/statusLogStore'
import { useWorktreeStore } from '../store/worktreeStore'
import { entryFullDate, entryTime, toggleStatusLog } from '../statusLog/statusLogActions'
import { Icon } from './Icon'
import { IconName } from './iconName'
import { Spinner } from './Spinner'
import { STATUS_LOG_ID, StatusLogDrawer } from './StatusLogDrawer'

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
  const logOpen = useStatusLogStore((state) => state.open)
  const spinning = Boolean(busy) || worktreeBusy

  return (
    <>
      {logOpen && <StatusLogDrawer />}
      <footer className={`flex h-[24px] shrink-0 items-center border-t border-dock-line bg-dock-paper font-mono text-[11px] ${spinning ? 'text-dock-ink-soft' : STATUS_CLASSES[status.level]}`}>
        <button
          type="button"
          data-status-log-toggle=""
          aria-expanded={logOpen}
          aria-controls={logOpen ? STATUS_LOG_ID : undefined}
          className="flex h-full min-w-0 flex-1 cursor-pointer items-center gap-[6px] px-3 text-left hover:bg-dock-green-hover"
          onClick={toggleStatusLog}
        >
          <span className="flex shrink-0" data-tip={logOpen ? 'Masquer le journal (Ctrl + Maj + L)' : 'Afficher le journal des messages (Ctrl + Maj + L)'}>
            <Icon name={IconName.Chevron} size={10} className={`text-dock-muted transition-transform ${logOpen ? 'rotate-90' : '-rotate-90'}`} />
          </span>
          {spinning && <Spinner size={10} className="shrink-0 text-dock-green" />}
          <span className="truncate py-1 [text-box:trim-both_cap_alphabetic]">{busy ? busyLabel(busy, busyRefs) : status.text}</span>
        </button>
        {status.at && !spinning && (
          <time dateTime={status.at} data-tip={`Message du ${entryFullDate(status)}`} className="shrink-0 px-3 text-dock-muted tabular-nums">
            {entryTime(status, new Date())}
          </time>
        )}
        {unsaved && (
          <span className="shrink-0 px-3 text-dock-error" data-tip="La dernière sauvegarde a échoué : la session restera en l’état d’avant tant qu’une écriture ne réussit pas.">
            Non enregistré
          </span>
        )}
      </footer>
    </>
  )
}
