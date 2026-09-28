import type { GitState } from '../bridge/gitMessages'
import { headSummary, plural } from '../git/gitLabels'
import { fetchRemote, pullBranch, pushBranch, refreshRepository, undoLastOperation } from '../git/gitRequests'
import { toggleGitGraph } from '../panel/rightPanel'
import { useGitStore } from '../store/gitStore'
import { GitAheadBehind } from './GitAheadBehind'
import { GitToolButton } from './GitToolButton'
import { Icon } from './Icon'
import { IconName } from './iconName'

interface GitHeaderProps {
  state: GitState
  busy: string | null
}

const undoTip = ({ undo }: GitState): string => {
  if (!undo) {
    return 'Aucune opération faite depuis Dock à annuler'
  }
  return undo.available ? `Annuler : ${undo.label}` : `Annulation impossible (${undo.reason ?? 'état inconnu'}) : ${undo.label}`
}

export function GitHeader({ state, busy }: GitHeaderProps) {
  const { head, remotes } = state
  const graphOpen = useGitStore((store) => store.graphOpen)
  const working = busy !== null
  const noRemote = remotes.length === 0
  const trackingTip = head.upstream ? `${plural(head.ahead, 'commit', 'commits')} à push, ${plural(head.behind, 'commit', 'commits')} à pull depuis ${head.upstream}` : ''
  const pushTip = head.detached
    ? 'HEAD détachée : faites le checkout d’une branche avant le push'
    : head.upstream
      ? `Push vers ${head.upstream}`
      : noRemote
        ? 'Aucun dépôt distant configuré'
        : `Push de « ${head.branch ?? ''} » : publie la branche sur le dépôt distant`

  return (
    <div className="flex shrink-0 flex-col gap-[4px] px-[10px] pb-[6px]">
      <div className="flex min-w-0 items-center gap-[8px] pl-[2px] text-[12px]">
        <span className="min-w-0 shrink truncate font-semibold text-dock-ink" data-tip={state.root}>
          {state.name}
        </span>
        <span className="flex min-w-0 items-center gap-[4px] font-mono text-dock-green-deep">
          <Icon name={IconName.Branch} className="shrink-0" />
          <span className="truncate">{headSummary(head)}</span>
        </span>
        {head.upstream && (head.ahead > 0 || head.behind > 0) && (
          <GitAheadBehind ahead={head.ahead} behind={head.behind} tip={trackingTip} />
        )}
        {!head.upstream && head.branch && !head.unborn && (
          <span className="shrink-0 text-[11px] text-dock-muted" data-tip="Aucune branche distante suivie : Push la publie">
            non publiée
          </span>
        )}
        <span className="flex-1" />
        <GitToolButton icon={IconName.Graph} label="Graphe" tip={graphOpen ? 'Masquer le graphe et revenir aux terminaux' : 'Afficher le graphe des branches et des commits à la place des terminaux'} pressed={graphOpen} onClick={toggleGitGraph} />
      </div>
      <div className="flex flex-wrap items-center gap-[2px]">
        <GitToolButton icon={IconName.Fetch} label="Fetch" tip={noRemote ? 'Aucun dépôt distant configuré' : 'Fetch de toutes les branches distantes (git fetch --all)'} disabled={working || noRemote} onClick={fetchRemote} />
        <GitToolButton icon={IconName.Pull} label={head.behind > 0 ? `Pull ${head.behind}` : 'Pull'} tip={head.upstream ? `Pull depuis ${head.upstream}` : 'Aucune branche distante suivie'} disabled={working || !head.upstream} onClick={pullBranch} />
        <GitToolButton icon={IconName.Push} label={head.ahead > 0 ? `Push ${head.ahead}` : 'Push'} tip={pushTip} disabled={working || head.detached || head.unborn || noRemote} onClick={pushBranch} />
        <span className="flex-1" />
        <GitToolButton icon={IconName.Undo} label="Annuler" tip={undoTip(state)} disabled={working || !state.undo?.available} onClick={undoLastOperation} />
        <GitToolButton icon={IconName.Refresh} tip="Actualiser" onClick={refreshRepository} />
      </div>
    </div>
  )
}
