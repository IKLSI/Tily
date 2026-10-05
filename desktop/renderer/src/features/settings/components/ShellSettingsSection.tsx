import type { ChangeEvent } from 'react'
import type { SettingsSnapshot } from '../../../bridge/messages'
import { InfoTip } from '../../../components/InfoTip'
import { BrowseButton } from './BrowseButton'
import { SETTINGS_INPUT, SETTINGS_LABEL, SETTINGS_ROW } from './settingsStyles'

interface ShellSettingsSectionProps {
  sectionClassName: string
  shellSettings: SettingsSnapshot['shellSettings']
  shells: Record<string, string>
  onShellChange: (shellId: string, path: string) => void
  onPickShell: (shellId: string) => void
}

export function ShellSettingsSection({ sectionClassName, shellSettings, shells, onShellChange, onPickShell }: ShellSettingsSectionProps) {
  return (
    <section className="flex flex-col gap-2">
      <div className={SETTINGS_ROW}>
        <h3 className={sectionClassName}>Shells</h3>
        <InfoTip text="Vide : chemin par défaut, affiché en filigrane. Un chemin introuvable est enregistré, mais le shell reste indisponible." />
      </div>
      {shellSettings.map((shell) => {
        const handleChange = (event: ChangeEvent<HTMLInputElement>) => onShellChange(shell.id, event.target.value)
        const handlePick = () => onPickShell(shell.id)
        return (
          <label key={shell.id} className="flex flex-col gap-1">
            <span className="flex items-baseline justify-between">
              <span className={SETTINGS_LABEL}>{shell.name}</span>
              <span className={`text-[11px] ${shell.available ? 'text-tily-green' : 'text-tily-warning'}`}>{shell.available ? 'Disponible' : 'Introuvable'}</span>
            </span>
            <span className="flex gap-1">
              <input type="text" className={SETTINGS_INPUT} value={shells[shell.id] ?? ''} placeholder={shell.defaultExecutable} spellCheck={false} onChange={handleChange} />
              <BrowseButton tip={`Choisir l’exécutable de ${shell.name}`} onClick={handlePick} />
            </span>
          </label>
        )
      })}
    </section>
  )
}
