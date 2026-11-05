import type { ChangeEvent } from 'react'
import { TerminalFont } from '../../../bridge/messages'
import { DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, MAX_FONT_SIZE, MIN_FONT_SIZE, fontStack } from '../../../model/appearance'
import { NUMBER_FIELDS, numberFieldId, valueWithin, type NumberField } from './persistenceFields'
import { SettingsField } from './SettingsField'
import { SettingsPanel } from './SettingsPanel'
import { SETTINGS_INPUT, SETTINGS_INPUT_BASE } from './settingsStyles'

interface TerminalSettingsSectionProps {
  title: string
  fontSize: number
  fontFamily: TerminalFont
  numberText: (field: NumberField) => string
  onFontSizeChange: (fontSize: number) => void
  onFontFamilyChange: (fontFamily: TerminalFont) => void
  onNumberChange: (field: NumberField, text: string) => void
}

const FONT_SIZES = Array.from({ length: MAX_FONT_SIZE - MIN_FONT_SIZE + 1 }, (_, index) => MIN_FONT_SIZE + index)
const FONT_FAMILIES = Object.values(TerminalFont)
const FONT_FAMILY_ID = 'settings-font-family'
const FONT_SIZE_ID = 'settings-font-size'
const NUMBER_BOX = 'w-[110px] shrink-0'
const INVALID_INPUT = `${SETTINGS_INPUT_BASE} border-tily-error focus:border-tily-error`

export function TerminalSettingsSection({ title, fontSize, fontFamily, numberText, onFontSizeChange, onFontFamilyChange, onNumberChange }: TerminalSettingsSectionProps) {
  const handleFontSizeChange = (event: ChangeEvent<HTMLSelectElement>) => onFontSizeChange(Number(event.target.value))
  const handleFontFamilyChange = (event: ChangeEvent<HTMLSelectElement>) => onFontFamilyChange(event.target.value as TerminalFont)

  return (
    <SettingsPanel title={title}>
      <SettingsField label="Police" htmlFor={FONT_FAMILY_ID}>
        <select id={FONT_FAMILY_ID} className={SETTINGS_INPUT} value={fontFamily} onChange={handleFontFamilyChange}>
          {FONT_FAMILIES.map((family) => (
            <option key={family} value={family}>
              {family === DEFAULT_FONT_FAMILY ? `${family} (par défaut)` : family}
            </option>
          ))}
        </select>
      </SettingsField>
      <SettingsField label="Taille du texte" tip="En pixels, pour tous les terminaux." htmlFor={FONT_SIZE_ID}>
        <span className="flex items-center gap-2">
          <span className="w-[190px] shrink-0">
            <select id={FONT_SIZE_ID} className={SETTINGS_INPUT} value={fontSize} onChange={handleFontSizeChange}>
              {FONT_SIZES.map((size) => (
                <option key={size} value={size}>
                  {size === DEFAULT_FONT_SIZE ? `${size} (par défaut)` : size}
                </option>
              ))}
            </select>
          </span>
          <span className="min-w-0 flex-1 overflow-hidden rounded-tily-sm border border-tily-line bg-tily-terminal px-2 py-1 whitespace-nowrap text-tily-terminal-ink" style={{ fontFamily: fontStack(fontFamily), fontSize }}>
            ~/Developer % git status
          </span>
        </span>
      </SettingsField>
      {NUMBER_FIELDS.map((field) => {
        const text = numberText(field)
        const invalid = valueWithin(text, field) === null
        const id = numberFieldId(field.key)
        const bounds = `Entre ${field.min} et ${field.max}.`
        const handleChange = (event: ChangeEvent<HTMLInputElement>) => onNumberChange(field, event.target.value)
        return (
          <SettingsField key={field.key} label={field.label} tip={`${field.hint} ${bounds}`} htmlFor={id}>
            <span className="flex items-center gap-2">
              <span className={NUMBER_BOX}>
                <input id={id} type="number" className={invalid ? INVALID_INPUT : SETTINGS_INPUT} value={text} min={field.min} max={field.max} aria-invalid={invalid} onChange={handleChange} />
              </span>
              {field.unit && <span className="text-tily-sm text-tily-muted">{field.unit}</span>}
              {invalid && <span className="text-tily-xs text-tily-error">{bounds}</span>}
            </span>
          </SettingsField>
        )
      })}
    </SettingsPanel>
  )
}
