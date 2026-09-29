import type { ChangeEvent } from 'react'
import { UpdateStatus, type UpdateInfo } from '../bridge/updateMessages'
import { useUpdateStore } from '../store/updateStore'
import { checkForUpdates, downloadPercent, formatTime, showUpdateDialog } from '../update/updateActions'
import { SETTINGS_BUTTON, SETTINGS_HINT, SETTINGS_LABEL, SETTINGS_SECONDARY } from './settingsStyles'
import { Spinner } from './Spinner'

interface UpdateSettingsSectionProps {
  sectionClassName: string
  autoCheck: boolean
  file?: string
  onAutoCheckChange: (autoCheck: boolean) => void
}

const PRIMARY = `${SETTINGS_BUTTON} border-dock-green text-dock-green-deep hover:bg-dock-green-soft`

const checkedSuffix = (info: UpdateInfo): string => (info.checkedAt ? ` · vérifié à ${formatTime(info.checkedAt)}` : '')

const describe = (info: UpdateInfo | null): { text: string; tone: string } => {
  if (!info || info.status === UpdateStatus.Idle) {
    return { text: 'Aucune vérification depuis le lancement de Dock.', tone: 'text-dock-muted' }
  }
  switch (info.status) {
    case UpdateStatus.Checking:
      return { text: 'Vérification auprès de GitHub…', tone: 'text-dock-muted' }
    case UpdateStatus.UpToDate:
      return { text: `Dock ${info.current} est à jour${checkedSuffix(info)}.`, tone: 'text-dock-green' }
    case UpdateStatus.Downloading:
      return { text: `Téléchargement de Dock ${info.release?.version ?? ''} : ${downloadPercent(info)} %`, tone: 'text-dock-ink' }
    case UpdateStatus.Ready:
      return { text: `Dock ${info.release?.version ?? ''} est téléchargé et vérifié, prêt à installer.`, tone: 'text-dock-green' }
    case UpdateStatus.Failed:
      return { text: info.error ?? 'La dernière vérification a échoué.', tone: 'text-dock-error' }
    default:
      return { text: `Dock ${info.release?.version ?? ''} est disponible (version actuelle ${info.current})${checkedSuffix(info)}.`, tone: 'text-dock-green' }
  }
}

export function UpdateSettingsSection({ sectionClassName, autoCheck, file, onAutoCheckChange }: UpdateSettingsSectionProps) {
  const info = useUpdateStore((state) => state.info)
  const { text, tone } = describe(info)
  const busy = info?.status === UpdateStatus.Checking || info?.status === UpdateStatus.Downloading
  const handleAutoCheckChange = (event: ChangeEvent<HTMLInputElement>) => onAutoCheckChange(event.target.checked)

  return (
    <section className="flex flex-col gap-2">
      <h3 className={sectionClassName}>Mises à jour</h3>
      <label className="flex items-center gap-2">
        <input type="checkbox" checked={autoCheck} onChange={handleAutoCheckChange} />
        <span className={SETTINGS_LABEL}>Rechercher les nouvelles versions au démarrage puis toutes les 6 heures</span>
      </label>
      <p className={SETTINGS_HINT}>Dock interroge les releases publiques du dépôt GitHub. Rien n’est téléchargé ni installé sans votre clic sur « Installer et redémarrer ».</p>
      <span className="flex items-center gap-3">
        {busy && <Spinner size={11} className="text-dock-green" />}
        <span className={`min-w-0 flex-1 text-[12px] ${tone}`}>{text}</span>
        {info?.release && (
          <button type="button" className={PRIMARY} onClick={showUpdateDialog}>
            Voir la mise à jour
          </button>
        )}
        <button type="button" className={SETTINGS_SECONDARY} aria-disabled={busy} onClick={busy ? undefined : checkForUpdates}>
          Rechercher maintenant
        </button>
      </span>
      {file && <p className={`${SETTINGS_HINT} font-mono`}>{file}</p>}
    </section>
  )
}
