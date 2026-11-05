import { type ChangeEvent } from 'react'
import { bridge } from '../../../bridge/bridge'
import type { ColorScheme } from '../../../bridge/appearanceMessages'
import type { AppearanceSettings } from '../../../bridge/messages'
import { folderName } from '../../../model/session'
import { useAppearanceStore } from '../../../stores/appearanceStore'
import { AccentSwatches } from '../../appearance/components/AccentSwatches'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { SettingsField } from './SettingsField'
import { SettingsPanel } from './SettingsPanel'
import { SETTINGS_HINT, SETTINGS_ICON_BUTTON, SETTINGS_INPUT } from './settingsStyles'

interface AppearanceSettingsSectionProps {
  title: string
  appearance: AppearanceSettings
  onChange: (patch: Partial<AppearanceSettings>) => void
}

const COLOR_SCHEMES: { value: ColorScheme; label: string }[] = [
  { value: 'system', label: 'Système (par défaut)' },
  { value: 'light', label: 'Clair' },
  { value: 'dark', label: 'Sombre' },
]
const SCHEME_TIP = 'Apparence de l’interface et des terminaux. « Système » suit le réglage de macOS.'
const ACCENT_TIP = 'Accents, fonds légèrement teintés, terminaux et barre de titre de toute l’application.'
const PROJECTS_TIP = 'Choisies depuis le menu d’un workspace, « Couleur… ». Elles surlignent le nom de tout workspace dont le pane actif est dans le projet, worktrees compris.'

export function AppearanceSettingsSection({ title, appearance, onChange }: AppearanceSettingsSectionProps) {
  const presets = useAppearanceStore((store) => store.accentPresets)
  const defaultColor = useAppearanceStore((store) => store.state?.defaultColor ?? '')
  const projects = Object.entries(appearance.projectColors).sort(([first], [second]) => folderName(first).localeCompare(folderName(second)))

  const handleAccentChange = (accent: string | undefined) => {
    onChange({ accent })
    bridge.send({ type: 'appearance.preview', color: accent, colorScheme: appearance.colorScheme })
  }
  const handleSchemeChange = (event: ChangeEvent<HTMLSelectElement>) => {
    const colorScheme = event.target.value as ColorScheme
    onChange({ colorScheme })
    bridge.send({ type: 'appearance.preview', color: appearance.accent, colorScheme })
  }

  return (
    <SettingsPanel title={title}>
      <SettingsField label="Thème" tip={SCHEME_TIP} htmlFor="color-scheme">
        <select id="color-scheme" className={SETTINGS_INPUT} value={appearance.colorScheme} onChange={handleSchemeChange}>
          {COLOR_SCHEMES.map(({ value, label }) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </SettingsField>
      <SettingsField label="Couleur de Tily" tip={ACCENT_TIP}>
        <AccentSwatches presets={presets} value={appearance.accent} fallback={defaultColor} fallbackLabel="Par défaut" onChange={handleAccentChange} />
      </SettingsField>
      <SettingsField label="Couleurs des projets" tip={PROJECTS_TIP}>
        {projects.length === 0 && <span className={SETTINGS_HINT}>Aucune.</span>}
        {projects.map(([project, color]) => {
          const remove = `Retirer la couleur de ${folderName(project)}`
          const handleRemove = () => onChange({ projectColors: Object.fromEntries(Object.entries(appearance.projectColors).filter(([candidate]) => candidate !== project)) })
          return (
            <span key={project} className="flex items-center gap-2">
              <span className="h-4 w-4 shrink-0 rounded-full border border-tily-line" style={{ backgroundColor: color }} />
              <span className="min-w-0 flex-1 truncate text-tily-sm text-tily-ink" data-tip={project}>
                {folderName(project)}
              </span>
              <button type="button" className={SETTINGS_ICON_BUTTON} aria-label={remove} data-tip={remove} onClick={handleRemove}>
                <Icon name={IconName.Close} />
              </button>
            </span>
          )
        })}
      </SettingsField>
    </SettingsPanel>
  )
}
