import type { ChangeEvent } from 'react'
import { TerminalFont } from '../../../bridge/messages'
import { DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, MAX_FONT_SIZE, MIN_FONT_SIZE, fontStack } from '../../../model/appearance'
import { SETTINGS_INPUT, SETTINGS_LABEL } from './settingsStyles'

interface AppearanceSettingsSectionProps {
  sectionClassName: string
  fontSize: number
  fontFamily: TerminalFont
  onFontSizeChange: (fontSize: number) => void
  onFontFamilyChange: (fontFamily: TerminalFont) => void
}

const FONT_SIZES = Array.from({ length: MAX_FONT_SIZE - MIN_FONT_SIZE + 1 }, (_, index) => MIN_FONT_SIZE + index)
const FONT_FAMILIES = Object.values(TerminalFont)

export function AppearanceSettingsSection({ sectionClassName, fontSize, fontFamily, onFontSizeChange, onFontFamilyChange }: AppearanceSettingsSectionProps) {
  const handleChange = (event: ChangeEvent<HTMLSelectElement>) => onFontSizeChange(Number(event.target.value))
  const handleFamilyChange = (event: ChangeEvent<HTMLSelectElement>) => onFontFamilyChange(event.target.value as TerminalFont)

  return (
    <section className="flex flex-col gap-2">
      <h3 className={sectionClassName}>Terminaux</h3>
      <label className="flex flex-col gap-1">
        <span className={SETTINGS_LABEL}>Police</span>
        <select className={SETTINGS_INPUT} value={fontFamily} onChange={handleFamilyChange}>
          {FONT_FAMILIES.map((family) => (
            <option key={family} value={family}>
              {family === DEFAULT_FONT_FAMILY ? `${family} (par défaut)` : family}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1">
        <span className={SETTINGS_LABEL}>Taille du texte (px)</span>
        <select className={SETTINGS_INPUT} value={fontSize} onChange={handleChange}>
          {FONT_SIZES.map((size) => (
            <option key={size} value={size}>
              {size === DEFAULT_FONT_SIZE ? `${size} (par défaut)` : size}
            </option>
          ))}
        </select>
      </label>
      <p className="overflow-hidden rounded border border-tily-line bg-tily-terminal px-2 py-1 whitespace-nowrap text-tily-terminal-ink" style={{ fontFamily: fontStack(fontFamily), fontSize }}>
        ~/Projects % git status
      </p>
    </section>
  )
}
