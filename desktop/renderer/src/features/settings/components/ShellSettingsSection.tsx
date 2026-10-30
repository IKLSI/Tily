import type { ChangeEvent } from 'react'
import type { SettingsSnapshot } from '../../../bridge/messages'
import { BrowseButton } from './BrowseButton'
import { SettingsField } from './SettingsField'
import { SettingsPanel } from './SettingsPanel'
import { SETTINGS_INPUT } from './settingsStyles'

interface ShellSettingsSectionProps {
  title: string
  shellSettings: SettingsSnapshot['shellSettings']
  shells: Record<string, string>
  editor: string
  onShellChange: (shellId: string, path: string) => void
  onEditorChange: (editor: string) => void
  onPickShell: (shellId: string) => void
  onPickEditor: () => void
}

const EDITOR_ID = 'settings-editor'
const AVAILABLE = 'Disponible'
const MISSING = 'Introuvable'

export function ShellSettingsSection({ title, shellSettings, shells, editor, onShellChange, onEditorChange, onPickShell, onPickEditor }: ShellSettingsSectionProps) {
  const handleEditorChange = (event: ChangeEvent<HTMLInputElement>) => onEditorChange(event.target.value)

  return (
    <SettingsPanel title={title} tip="Vide : chemin par défaut, affiché en filigrane. Un chemin introuvable est enregistré, mais le shell reste indisponible.">
      {shellSettings.map((shell) => {
        const id = `settings-shell-${shell.id}`
        const handleChange = (event: ChangeEvent<HTMLInputElement>) => onShellChange(shell.id, event.target.value)
        const handlePick = () => onPickShell(shell.id)
        const status = shell.available ? AVAILABLE : MISSING
        const marker = <span className={`size-1.5 shrink-0 rounded-full ${shell.available ? 'bg-tily-green' : 'bg-tily-warning'}`} role="img" aria-label={status} data-tip={status} />
        return (
          <SettingsField key={shell.id} label={shell.name} htmlFor={id} marker={marker}>
            <span className="flex gap-1">
              <input id={id} type="text" className={SETTINGS_INPUT} value={shells[shell.id] ?? ''} placeholder={shell.defaultExecutable} spellCheck={false} onChange={handleChange} />
              <BrowseButton tip={`Choisir l’exécutable de ${shell.name}`} onClick={handlePick} />
            </span>
            {!shell.available && <span className="text-[11px] text-tily-warning">Introuvable : le chemin est enregistré, mais le shell reste indisponible.</span>}
          </SettingsField>
        )
      })}
      <SettingsField label="Éditeur" tip="Commande d’ouverture d’un dossier." htmlFor={EDITOR_ID}>
        <span className="flex gap-1">
          <input id={EDITOR_ID} type="text" className={SETTINGS_INPUT} value={editor} placeholder="code" spellCheck={false} onChange={handleEditorChange} />
          <BrowseButton tip="Choisir l’exécutable de l’éditeur" onClick={onPickEditor} />
        </span>
      </SettingsField>
    </SettingsPanel>
  )
}
