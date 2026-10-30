import type { ChangeEvent } from 'react'
import { NotificationSound } from '../../../bridge/messages'
import { Icon } from '../../../components/Icon'
import { IconName } from '../../../components/iconName'
import { SETTINGS_BROWSE, SETTINGS_INPUT, SETTINGS_SECONDARY } from './settingsStyles'

interface SoundSettingProps {
  label: string
  sound: string
  placeholder: string
  testTip: string
  onChange: (sound: string) => void
  onPick: () => void
  onTest: () => void
}

const CUSTOM_SOUND = 'custom'
const PICK_TIP = 'Choisir un fichier son (.aiff, .wav, .mp3)'

const SOUND_LABELS: Record<NotificationSound, string> = {
  [NotificationSound.None]: 'Aucun',
  [NotificationSound.Basso]: 'Basso',
  [NotificationSound.Blow]: 'Blow',
  [NotificationSound.Bottle]: 'Bottle',
  [NotificationSound.Frog]: 'Frog',
  [NotificationSound.Funk]: 'Funk',
  [NotificationSound.Glass]: 'Glass',
  [NotificationSound.Hero]: 'Hero',
  [NotificationSound.Morse]: 'Morse',
  [NotificationSound.Ping]: 'Ping',
  [NotificationSound.Pop]: 'Pop',
  [NotificationSound.Purr]: 'Purr',
  [NotificationSound.Sosumi]: 'Sosumi',
  [NotificationSound.Submarine]: 'Submarine',
  [NotificationSound.Tink]: 'Tink',
}

const isCustomSound = (sound: string): boolean => !Object.values<string>(NotificationSound).includes(sound)

export function SoundSetting({ label, sound, placeholder, testTip, onChange, onPick, onTest }: SoundSettingProps) {
  const custom = isCustomSound(sound)
  const handleSelect = (event: ChangeEvent<HTMLSelectElement>) => onChange(event.target.value === CUSTOM_SOUND ? '' : event.target.value)
  const handlePathChange = (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value)

  return (
    <>
      <span className="flex gap-1">
        <select className={SETTINGS_INPUT} value={custom ? CUSTOM_SOUND : sound} aria-label={label} onChange={handleSelect}>
          {Object.values(NotificationSound).map((known) => (
            <option key={known} value={known}>
              {SOUND_LABELS[known]}
            </option>
          ))}
          <option value={CUSTOM_SOUND}>Fichier son de mon ordinateur…</option>
        </select>
        <button type="button" className={`${SETTINGS_SECONDARY} shrink-0`} data-tip={testTip} onClick={onTest}>
          Tester
        </button>
      </span>
      {custom && (
        <span className="flex gap-1">
          <input type="text" className={SETTINGS_INPUT} value={sound} placeholder={placeholder} spellCheck={false} aria-label={`${label} : fichier son`} onChange={handlePathChange} />
          <button type="button" className={SETTINGS_BROWSE} aria-label={PICK_TIP} data-tip={PICK_TIP} onClick={onPick}>
            <Icon name={IconName.More} />
          </button>
        </span>
      )}
    </>
  )
}
