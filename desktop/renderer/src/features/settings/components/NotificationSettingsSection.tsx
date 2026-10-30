import type { ChangeEvent } from 'react'
import { AttentionKind, type NotificationSettings } from '../../../bridge/messages'
import { InfoTip } from '../../../components/InfoTip'
import { SettingsField } from './SettingsField'
import { SettingsPanel } from './SettingsPanel'
import { SETTINGS_CHECKBOX, SETTINGS_ROW } from './settingsStyles'
import { SoundSetting } from './SoundSetting'

interface NotificationSettingsSectionProps {
  title: string
  notifications: NotificationSettings
  onChange: (patch: Partial<NotificationSettings>) => void
  onPickSound: () => void
  onPickDoneSound: () => void
  onTest: (kind: AttentionKind) => void
}

const CHECKBOX_ROW = `${SETTINGS_ROW} min-h-[30px]`
const SOUND_TIP = 'Un fichier .aiff, .wav ou .mp3 se donne par son chemin absolu ; un chemin invalide revient au son par défaut à l’enregistrement.'

export function NotificationSettingsSection({ title, notifications, onChange, onPickSound, onPickDoneSound, onTest }: NotificationSettingsSectionProps) {
  const handleSystemNotificationChange = (event: ChangeEvent<HTMLInputElement>) => onChange({ systemNotification: event.target.checked })
  const handleDockBounceChange = (event: ChangeEvent<HTMLInputElement>) => onChange({ dockBounce: event.target.checked })
  const handleNotifyDoneChange = (event: ChangeEvent<HTMLInputElement>) => onChange({ notifyDone: event.target.checked })
  const handleSoundChange = (sound: string) => onChange({ sound })
  const handleDoneSoundChange = (doneSound: string) => onChange({ doneSound })
  const handleTestWaiting = () => onTest(AttentionKind.Waiting)
  const handleTestDone = () => onTest(AttentionKind.Done)

  return (
    <SettingsPanel title={title}>
      <SettingsField label="Attente d’un agent" tip="Quand un agent a besoin de vous et que Tily n’est pas la fenêtre active ; les cartes dans Tily restent toujours affichées.">
        <div className="flex flex-col">
          <span className={CHECKBOX_ROW}>
            <label className={SETTINGS_CHECKBOX}>
              <input type="checkbox" checked={notifications.systemNotification} onChange={handleSystemNotificationChange} />
              Notification macOS
            </label>
            <InfoTip text="Un clic sur la notification rejoint le terminal de l’agent." />
          </span>
          <span className={CHECKBOX_ROW}>
            <label className={SETTINGS_CHECKBOX}>
              <input type="checkbox" checked={notifications.dockBounce} onChange={handleDockBounceChange} />
              Rebond de l’icône dans le Dock
            </label>
            <InfoTip text="Aussi à la fin d’une commande de plus de 10 s." />
          </span>
        </div>
      </SettingsField>
      <SettingsField label="Son d’attente" tip={SOUND_TIP}>
        <SoundSetting label="Son d’attente" sound={notifications.sound} placeholder="/Users/vous/Music/attention.aiff" testTip="Joue le son, fait rebondir l’icône dans le Dock et affiche la notification macOS si elle est disponible, avec les réglages ci-dessus, sans enregistrer" onChange={handleSoundChange} onPick={onPickSound} onTest={handleTestWaiting} />
      </SettingsField>
      <SettingsField label="Fin d’un agent" tip="Chaque fois qu’un agent termine, avec son dernier message, même si Tily est la fenêtre active.">
        <label className={SETTINGS_CHECKBOX}>
          <input type="checkbox" checked={notifications.notifyDone} onChange={handleNotifyDoneChange} />
          Notifier
        </label>
      </SettingsField>
      {notifications.notifyDone && (
        <SettingsField label="Son de fin" tip={SOUND_TIP}>
          <SoundSetting label="Son de fin" sound={notifications.doneSound} placeholder="/Users/vous/Music/termine.aiff" testTip="Joue le son de fin et affiche la notification de fin, avec les réglages ci-dessus, sans enregistrer" onChange={handleDoneSoundChange} onPick={onPickDoneSound} onTest={handleTestDone} />
        </SettingsField>
      )}
    </SettingsPanel>
  )
}
