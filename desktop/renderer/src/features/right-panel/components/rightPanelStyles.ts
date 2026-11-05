import { BUTTON_DANGER_SMALL, BUTTON_PRIMARY_SMALL, BUTTON_SECONDARY_SMALL, FIELD, ICON_BUTTON } from '../../../components/controlStyles'

export const PANEL_HEADER_BUTTON = `${ICON_BUTTON} size-[24px]`

export const ROW_ACTION = `${ICON_BUTTON} size-[20px]`

const BOXED_BUTTON = 'flex shrink-0 cursor-pointer items-center justify-center rounded-[4px] border'

const STAGE_COLORS = 'border-tily-green bg-tily-green-soft text-tily-green-deep hover:bg-tily-green hover:text-tily-paper'

const UNSTAGE_COLORS = 'border-tily-error bg-tily-diff-removed text-tily-diff-removed-ink hover:bg-tily-error hover:text-tily-paper'

export const DIFF_STAGE_BUTTON = `${BOXED_BUTTON} size-[16px] ${STAGE_COLORS}`

export const DIFF_UNSTAGE_BUTTON = `${BOXED_BUTTON} size-[16px] ${UNSTAGE_COLORS}`

export const ROW_STAGE_BUTTON = `${BOXED_BUTTON} mx-[1px] size-[18px] ${STAGE_COLORS}`

export const ROW_UNSTAGE_BUTTON = `${BOXED_BUTTON} mx-[1px] size-[18px] ${UNSTAGE_COLORS}`

export const SECTION_TITLE = 'text-tily-xs font-semibold tracking-[0.06em] text-tily-muted uppercase'

export const SECTION_HEADING = 'text-tily-xs font-semibold tracking-[0.06em] text-tily-ink uppercase'

export const GIT_PRIMARY = BUTTON_PRIMARY_SMALL

export const GIT_SECONDARY = BUTTON_SECONDARY_SMALL

export const GIT_DANGER = BUTTON_DANGER_SMALL

export const GIT_INPUT = FIELD
