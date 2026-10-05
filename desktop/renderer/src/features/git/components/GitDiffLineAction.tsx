import { GitDiffSource } from '../../../bridge/gitMessages'
import { applyDiffLine, DiffLineAction } from '../gitDiffActions'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { DIFF_STAGE_BUTTON, DIFF_UNSTAGE_BUTTON } from '../../right-panel/components/rightPanelStyles'

interface GitDiffLineActionProps {
  row: number
  source: GitDiffSource
  inSelection: boolean
  shown: boolean
}

export function GitDiffLineAction({ row, source, inSelection, shown }: GitDiffLineActionProps) {
  const staged = source === GitDiffSource.Staged
  const tip = `${staged ? 'Unstage' : 'Stage'} ${inSelection ? 'des lignes choisies' : 'de la ligne'}`
  const handleClick = () => applyDiffLine(row, staged ? DiffLineAction.Unstage : DiffLineAction.Stage)

  return (
    <button type="button" tabIndex={-1} className={`${staged ? DIFF_UNSTAGE_BUTTON : DIFF_STAGE_BUTTON} ${shown ? '' : 'invisible group-hover/line:visible'}`} aria-label={tip} data-tip={tip} onClick={handleClick}>
      <Icon name={staged ? IconName.Minus : IconName.Plus} size={10} />
    </button>
  )
}
