import type { ChangeEvent, RefObject } from 'react'
import type { PersistenceSettings } from '../../../bridge/messages'
import { InfoTip } from '../../../components/InfoTip'
import { NUMBER_FIELDS, numberTextOf, valueWithin, type NumberTexts } from './persistenceFields'
import { SETTINGS_INPUT, SETTINGS_INPUT_BASE, SETTINGS_LABEL, SETTINGS_ROW } from './settingsStyles'

interface PersistenceSettingsSectionProps {
  sectionClassName: string
  persistence: PersistenceSettings
  numberTexts: NumberTexts
  inputs: RefObject<Partial<Record<keyof PersistenceSettings, HTMLInputElement | null>>>
  onNumberTextChange: (key: keyof PersistenceSettings, text: string) => void
  onPersistenceChange: (persistence: PersistenceSettings) => void
}

const INVALID_INPUT = `${SETTINGS_INPUT_BASE} border-tily-error focus:border-tily-error`
const INVALID_HINT = 'text-[11px] text-tily-error'

export function PersistenceSettingsSection({ sectionClassName, persistence, numberTexts, inputs, onNumberTextChange, onPersistenceChange }: PersistenceSettingsSectionProps) {
  return (
    <section className="flex flex-col gap-2">
      <h3 className={sectionClassName}>Persistance</h3>
      {NUMBER_FIELDS.map((field) => {
        const text = numberTextOf(numberTexts, persistence, field)
        const invalid = valueWithin(text, field) === null
        const id = `settings-${field.key}`
        const keepInput = (input: HTMLInputElement | null) => {
          inputs.current[field.key] = input
        }
        const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
          const next = event.target.value
          onNumberTextChange(field.key, next)
          const value = valueWithin(next, field)
          if (value !== null) {
            onPersistenceChange({ ...persistence, [field.key]: value })
          }
        }
        return (
          <div key={field.key} className="flex flex-col gap-1">
            <span className={SETTINGS_ROW}>
              <label htmlFor={id} className={SETTINGS_LABEL}>
                {field.label}
              </label>
              <InfoTip text={`${field.hint} Entre ${field.min} et ${field.max}.`} />
            </span>
            <input id={id} type="number" className={invalid ? INVALID_INPUT : SETTINGS_INPUT} value={text} min={field.min} max={field.max} aria-invalid={invalid} ref={keepInput} onChange={handleChange} />
            {invalid && <span className={INVALID_HINT}>{`Entre ${field.min} et ${field.max}.`}</span>}
          </div>
        )
      })}
    </section>
  )
}
