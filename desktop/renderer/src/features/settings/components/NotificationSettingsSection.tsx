import type { ChangeEvent } from 'react'
import { AttentionKind, type NotificationSettings } from '../../../bridge/messages'
import { InfoTip } from '../../../components/InfoTip'
import { SETTINGS_LABEL, SETTINGS_ROW } from './settingsStyles'
import { SoundSetting } from './SoundSetting'

interface NotificationSettingsSectionProps {
  sectionClassName: string
  notifications: NotificationSettings
  onChange: (patch: Partial<NotificationSettings>) => void
  onPickSound: () => void
  onPickDoneSound: () => void
  onTest: (kind: AttentionKind) => void
}

export function NotificationSettingsSection({ sectionClassName, notifications, onChange, onPickSound, onPickDoneSound, onTest }: NotificationSettingsSectionProps) {
  const handleSystemNotificationChange = (event: ChangeEvent<HTMLInputElement>) => onChange({ systemNotification: event.target.checked })
  const handleDockBounceChange = (event: ChangeEvent<HTMLInputElement>) => onChange({ dockBounce: event.target.checked })
  const handleNotifyDoneChange = (event: ChangeEvent<HTMLInputElement>) => onChange({ notifyDone: event.target.checked })
  const handleSoundChange = (sound: string) => onChange({ sound })
  const handleDoneSoundChange = (doneSound: string) => onChange({ doneSound })
  const handleTestWaiting = () => onTest(AttentionKind.Waiting)
  const handleTestDone = () => onTest(AttentionKind.Done)

  return (
    <section className="flex flex-col gap-2">
      <div className={SETTINGS_ROW}>
        <h3 className={sectionClassName}>Notifications</h3>
        <InfoTip text="Attente : quand un agent a besoin de vous et que Tily n’est pas la fenêtre active ; les cartes dans Tily restent toujours affichées. Fin : chaque fois qu’un agent termine, avec son dernier message." />
      </div>
      <span className={SETTINGS_ROW}>
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={notifications.systemNotification} onChange={handleSystemNotificationChange} />
          <span className={SETTINGS_LABEL}>Notification macOS</span>
        </label>
        <InfoTip text="Un clic sur la notification rejoint le terminal de l’agent." />
      </span>
      <span className={SETTINGS_ROW}>
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={notifications.dockBounce} onChange={handleDockBounceChange} />
          <span className={SETTINGS_LABEL}>Faire rebondir l’icône de Tily dans le Dock</span>
        </label>
        <InfoTip text="Aussi à la fin d’une commande de plus de 10 s." />
      </span>
      <SoundSetting label="Son joué à chaque nouvelle attente" sound={notifications.sound} placeholder="/Users/vous/Music/attention.aiff" testTip="Joue le son, fait rebondir l’icône dans le Dock et affiche la notification macOS si elle est disponible, avec les réglages ci-dessus, sans enregistrer" onChange={handleSoundChange} onPick={onPickSound} onTest={handleTestWaiting} />
      <span className={SETTINGS_ROW}>
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={notifications.notifyDone} onChange={handleNotifyDoneChange} />
          <span className={SETTINGS_LABEL}>Notifier quand un agent a terminé</span>
        </label>
        <InfoTip text="Même si Tily est la fenêtre active." />
      </span>
      {notifications.notifyDone && <SoundSetting label="Son joué quand un agent a terminé" sound={notifications.doneSound} placeholder="/Users/vous/Music/termine.aiff" testTip="Joue le son de fin et affiche la notification de fin, avec les réglages ci-dessus, sans enregistrer" onChange={handleDoneSoundChange} onPick={onPickDoneSound} onTest={handleTestDone} />}
    </section>
  )
}
