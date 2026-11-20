import { useEffect } from 'react'
import { useUpdateStore } from '../../updates/updateStore'
import { requestReleaseHistory, retryReleaseHistory } from '../../updates/updateActions'
import { Spinner } from '../../../components/Spinner'
import { ReleaseHistoryItem } from './ReleaseHistoryItem'
import { SettingsPanel } from './SettingsPanel'
import { SETTINGS_HINT, SETTINGS_SECONDARY } from './settingsStyles'

interface ReleaseNotesSettingsSectionProps {
  title: string
}

export function ReleaseNotesSettingsSection({ title }: ReleaseNotesSettingsSectionProps) {
  const history = useUpdateStore((state) => state.history)
  const loading = useUpdateStore((state) => state.historyLoading)
  const error = useUpdateStore((state) => state.historyError)
  const current = useUpdateStore((state) => state.info?.current)

  useEffect(requestReleaseHistory, [])

  return (
    <SettingsPanel title={title} tip="Versions publiées sur le dépôt GitHub de Tily, de la plus récente à la plus ancienne." grid={false}>
      {loading && (
        <p className="flex items-center gap-2 text-tily-sm text-tily-muted">
          <Spinner size={11} className="text-tily-green" />
          Chargement des versions depuis GitHub…
        </p>
      )}
      {error && !loading && (
        <div className="flex items-center gap-3">
          <p className="min-w-0 flex-1 text-tily-sm text-tily-error">{error}</p>
          <button type="button" className={SETTINGS_SECONDARY} onClick={retryReleaseHistory}>
            Réessayer
          </button>
        </div>
      )}
      {history?.length === 0 && !loading && <p className={SETTINGS_HINT}>Aucune version publiée.</p>}
      {history?.map((entry, index) => (
        <ReleaseHistoryItem key={entry.version} entry={entry} installed={entry.version === current} initiallyOpen={index === 0} />
      ))}
    </SettingsPanel>
  )
}
