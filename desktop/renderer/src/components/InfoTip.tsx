import { Icon } from './Icon'
import { IconName } from './iconName'

interface InfoTipProps {
  text: string
}

export function InfoTip({ text }: InfoTipProps) {
  return (
    <button type="button" className="inline-flex shrink-0 cursor-help rounded-full text-tily-muted hover:text-tily-ink focus-visible:text-tily-ink" aria-label={text} data-tip={text} data-tip-click="">
      <Icon name={IconName.Info} size={12} />
    </button>
  )
}
