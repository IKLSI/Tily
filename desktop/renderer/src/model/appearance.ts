import { TerminalFont } from '../bridge/messages'

export const DEFAULT_FONT_SIZE = 14
export const MIN_FONT_SIZE = 8
export const MAX_FONT_SIZE = 32
export const DEFAULT_FONT_FAMILY = TerminalFont.Menlo
const FALLBACK_FONTS = '"CaskaydiaCove Nerd Font Mono", "CaskaydiaCove Nerd Font", "Symbols Nerd Font Mono", Menlo, monospace'

export const fontStack = (family: string): string => `"${family}", ${FALLBACK_FONTS}`
