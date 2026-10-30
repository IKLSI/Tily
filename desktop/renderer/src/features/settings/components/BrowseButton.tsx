import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { SETTINGS_BROWSE } from './settingsStyles'

interface BrowseButtonProps {
  tip: string
  onClick: () => void
}

export function BrowseButton({ tip, onClick }: BrowseButtonProps) {
  return (
    <button type="button" className={SETTINGS_BROWSE} aria-label={tip} data-tip={tip} onClick={onClick}>
      <Icon name={IconName.More} />
    </button>
  )
}
