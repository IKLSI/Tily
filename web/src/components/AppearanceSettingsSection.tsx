import type { ChangeEvent } from 'react'
import { DEFAULT_FONT_SIZE, MAX_FONT_SIZE, MIN_FONT_SIZE } from '../model/appearance'
import { SETTINGS_HINT, SETTINGS_INPUT, SETTINGS_LABEL } from './settingsStyles'

interface AppearanceSettingsSectionProps {
  sectionClassName: string
  fontSize: number
  file?: string
  onFontSizeChange: (fontSize: number) => void
}

const FONT_SIZES = Array.from({ length: MAX_FONT_SIZE - MIN_FONT_SIZE + 1 }, (_, index) => MIN_FONT_SIZE + index)
const SAMPLE_FONT = '"CaskaydiaCove Nerd Font Mono", "Cascadia Mono", Consolas, monospace'

export function AppearanceSettingsSection({ sectionClassName, fontSize, file, onFontSizeChange }: AppearanceSettingsSectionProps) {
  const handleChange = (event: ChangeEvent<HTMLSelectElement>) => onFontSizeChange(Number(event.target.value))

  return (
    <section className="flex flex-col gap-2">
      <h3 className={sectionClassName}>Terminaux</h3>
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
      <p className="overflow-hidden rounded border border-dock-line bg-dock-terminal px-2 py-1 whitespace-nowrap text-dock-terminal-ink" style={{ fontFamily: SAMPLE_FONT, fontSize }}>
        PS C:\Files\Projects&gt; git status
      </p>
      <p className={SETTINGS_HINT}>S’applique à tous les terminaux ouverts dès l’enregistrement.</p>
      {file && <p className={`${SETTINGS_HINT} font-mono`}>{file}</p>}
    </section>
  )
}
