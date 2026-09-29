export const PANEL_HEADER_BUTTON =
  'flex size-[24px] shrink-0 cursor-pointer items-center justify-center rounded-md text-dock-muted hover:bg-dock-green-hover hover:text-dock-ink aria-disabled:cursor-default aria-disabled:opacity-40 aria-disabled:hover:bg-transparent aria-disabled:hover:text-dock-muted'

export const ROW_ACTION =
  'flex size-[20px] shrink-0 cursor-pointer items-center justify-center rounded text-dock-muted hover:bg-dock-panel hover:text-dock-ink'

const BOXED_BUTTON = 'flex shrink-0 cursor-pointer items-center justify-center rounded-[4px] border'

const STAGE_COLORS = 'border-dock-green bg-dock-green-soft text-dock-green-deep hover:bg-dock-green hover:text-dock-paper'

const UNSTAGE_COLORS = 'border-dock-error bg-dock-diff-removed text-dock-diff-removed-ink hover:bg-dock-error hover:text-dock-paper'

export const DIFF_STAGE_BUTTON = `${BOXED_BUTTON} size-[16px] ${STAGE_COLORS}`

export const DIFF_UNSTAGE_BUTTON = `${BOXED_BUTTON} size-[16px] ${UNSTAGE_COLORS}`

export const ROW_STAGE_BUTTON = `${BOXED_BUTTON} mx-[1px] size-[18px] ${STAGE_COLORS}`

export const ROW_UNSTAGE_BUTTON = `${BOXED_BUTTON} mx-[1px] size-[18px] ${UNSTAGE_COLORS}`

export const SECTION_TITLE = 'text-[11px] font-semibold tracking-[0.06em] text-dock-muted uppercase'

const GIT_BUTTON = 'cursor-pointer rounded border px-3 py-1 text-[12px] aria-disabled:cursor-default aria-disabled:opacity-50'

export const GIT_PRIMARY = `${GIT_BUTTON} border-dock-green text-dock-green-deep hover:bg-dock-green-soft aria-disabled:hover:bg-transparent`

export const GIT_SECONDARY = `${GIT_BUTTON} border-dock-line text-dock-ink hover:bg-dock-green-hover aria-disabled:hover:bg-transparent`

export const GIT_DANGER = `${GIT_BUTTON} border-dock-error text-dock-error hover:bg-dock-green-hover`

export const GIT_INPUT = 'w-full rounded border border-dock-line bg-dock-panel px-2 py-1 text-[12px] text-dock-ink outline-none placeholder:text-dock-muted focus:border-dock-focus'
