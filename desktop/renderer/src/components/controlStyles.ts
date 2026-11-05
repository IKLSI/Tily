const CONTROL = 'inline-flex shrink-0 cursor-pointer items-center justify-center gap-[6px] rounded-tily-sm border text-tily-sm font-medium select-none active:translate-y-px aria-disabled:cursor-default aria-disabled:opacity-60 aria-disabled:active:translate-y-0 disabled:cursor-default disabled:opacity-60 disabled:active:translate-y-0'

export const CONTROL_MD = `${CONTROL} h-[30px] px-[12px]`

export const CONTROL_SM = `${CONTROL} h-[24px] px-[10px]`

const PRIMARY_COLORS = 'border-tily-green/60 bg-tily-green-soft text-tily-green-deep hover:border-tily-green aria-disabled:hover:border-tily-green/60 disabled:hover:border-tily-green/60'

const SECONDARY_COLORS = 'border-tily-line text-tily-ink hover:border-tily-muted/50 hover:bg-tily-green-hover aria-disabled:hover:border-tily-line aria-disabled:hover:bg-transparent disabled:hover:bg-transparent'

const DANGER_COLORS = 'border-tily-error/70 text-tily-error hover:border-tily-error hover:bg-tily-error/10 aria-disabled:hover:bg-transparent disabled:hover:bg-transparent'

export const BUTTON_PRIMARY = `${CONTROL_MD} ${PRIMARY_COLORS}`

export const BUTTON_SECONDARY = `${CONTROL_MD} ${SECONDARY_COLORS}`

export const BUTTON_DANGER = `${CONTROL_MD} ${DANGER_COLORS}`

export const BUTTON_PRIMARY_SMALL = `${CONTROL_SM} ${PRIMARY_COLORS}`

export const BUTTON_SECONDARY_SMALL = `${CONTROL_SM} ${SECONDARY_COLORS}`

export const BUTTON_DANGER_SMALL = `${CONTROL_SM} ${DANGER_COLORS}`

export const ICON_BUTTON = 'flex shrink-0 cursor-pointer items-center justify-center rounded-tily-sm text-tily-muted hover:bg-tily-green-hover hover:text-tily-ink aria-disabled:cursor-default aria-disabled:opacity-60 aria-disabled:hover:bg-transparent aria-disabled:hover:text-tily-muted'

export const FIELD = 'w-full rounded-tily-sm border border-tily-line bg-tily-paper px-2 py-1.5 text-tily-sm text-tily-ink outline-none placeholder:text-tily-muted focus:border-tily-focus'
