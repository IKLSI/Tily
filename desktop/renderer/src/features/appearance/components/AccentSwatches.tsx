import type { AccentPreset } from '../../../bridge/appearanceMessages'
import { ColorInput } from './ColorInput'

interface AccentSwatchesProps {
  presets: AccentPreset[]
  value?: string
  fallback?: string
  fallbackLabel: string
  onChange: (color: string | undefined) => void
}

const SWATCH = 'h-6 w-6 shrink-0 cursor-pointer rounded-full border border-tily-line'
const SELECTED = 'outline-2 outline-offset-2 outline-tily-ink'
const FALLBACK = 'flex h-6 shrink-0 cursor-pointer items-center gap-1.5 rounded-full border border-tily-line px-2 text-tily-xs text-tily-ink-soft hover:bg-tily-green-hover'
const PICKER_START = '#808080'

export function AccentSwatches({ presets, value, fallback, fallbackLabel, onChange }: AccentSwatchesProps) {
  const custom = value !== undefined && !presets.some((preset) => preset.color === value)
  const handleFallback = () => onChange(undefined)

  return (
    <div role="group" aria-label="Couleur" className="flex flex-wrap items-center gap-2 py-1">
      {fallback ? (
        <button type="button" aria-label={fallbackLabel} aria-pressed={value === undefined} data-tip={fallbackLabel} className={value === undefined ? `${SWATCH} ${SELECTED}` : SWATCH} style={{ backgroundColor: fallback }} onClick={handleFallback} />
      ) : (
        <button type="button" aria-pressed={value === undefined} className={value === undefined ? `${FALLBACK} ${SELECTED}` : FALLBACK} onClick={handleFallback}>
          {fallbackLabel}
        </button>
      )}
      {presets.map((preset) => {
        const selected = preset.color === value
        const handleClick = () => onChange(preset.color)
        return <button key={preset.color} type="button" aria-label={preset.name} aria-pressed={selected} data-tip={preset.name} className={selected ? `${SWATCH} ${SELECTED}` : SWATCH} style={{ backgroundColor: preset.color }} onClick={handleClick} />
      })}
      <span className="flex rounded-full border border-dashed border-tily-muted p-0.5">
        <ColorInput value={value ?? fallback ?? PICKER_START} label="Couleur libre" selected={custom} onChange={onChange} />
      </span>
    </div>
  )
}
