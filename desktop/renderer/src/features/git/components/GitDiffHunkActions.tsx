import { GitDiffSource } from '../../../bridge/gitMessages'
import { applyDiffHunk, DiffLineAction } from '../gitDiffActions'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { DIFF_STAGE_BUTTON, DIFF_UNSTAGE_BUTTON } from '../../right-panel/components/rightPanelStyles'

interface GitDiffHunkActionsProps {
  hunk: number
  source: GitDiffSource
}

const HUNK_BUTTON = 'flex h-[16px] w-[20px] cursor-pointer items-center justify-center rounded text-tily-ink-soft hover:bg-tily-green-hover hover:text-tily-ink'

export function GitDiffHunkActions({ hunk, source }: GitDiffHunkActionsProps) {
  const staged = source === GitDiffSource.Staged
  const tip = staged ? 'Unstage du chunk' : 'Stage du chunk'
  const handleStageOrUnstage = () => applyDiffHunk(hunk, staged ? DiffLineAction.Unstage : DiffLineAction.Stage)
  const handleDiscard = () => applyDiffHunk(hunk, DiffLineAction.Discard)

  return (
    <>
      <span className="flex w-[24px] shrink-0 items-center justify-center">
        <button type="button" tabIndex={-1} className={staged ? DIFF_UNSTAGE_BUTTON : DIFF_STAGE_BUTTON} aria-label={tip} data-tip={tip} onClick={handleStageOrUnstage}>
          <Icon name={staged ? IconName.Minus : IconName.Plus} size={10} />
        </button>
      </span>
      <span className="flex w-[108px] shrink-0 items-center justify-end pr-[6px]">
        {!staged && (
          <button type="button" tabIndex={-1} className={HUNK_BUTTON} aria-label="Abandonner le chunk" data-tip="Abandonner le chunk" onClick={handleDiscard}>
            <Icon name={IconName.Discard} />
          </button>
        )}
      </span>
    </>
  )
}
