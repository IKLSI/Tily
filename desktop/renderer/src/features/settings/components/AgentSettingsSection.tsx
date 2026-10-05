import type { SettingsSnapshot } from '../../../bridge/messages'
import { InfoTip } from '../../../components/InfoTip'
import { SETTINGS_PRIMARY, SETTINGS_ROW, SETTINGS_SECONDARY } from './settingsStyles'

interface AgentSettingsSectionProps {
  sectionClassName: string
  agents: SettingsSnapshot['agents']
  onInstallHooks: () => void
  onRemoveHooks: () => void
}

export function AgentSettingsSection({ sectionClassName, agents, onInstallHooks, onRemoveHooks }: AgentSettingsSectionProps) {
  return (
    <section className="flex flex-col gap-2">
      <div className={SETTINGS_ROW}>
        <h3 className={sectionClassName}>Agents</h3>
        <InfoTip text={`Claude Code signale ses états (en cours, en attente, terminé, en erreur) par des hooks qui exécutent ${agents.script}. Sans hooks, un processus claude ou codex est affiché « État inconnu ». Les états sont écrits dans ${agents.stateDirectory} et purgés à chaque nouveau terminal.`} />
      </div>
      <span className="flex items-center gap-3">
        <span className={`text-[11px] ${agents.hooksInstalled ? 'text-tily-green' : 'text-tily-warning'}`}>{agents.hooksInstalled ? 'Hooks de Claude Code installés' : 'Hooks de Claude Code non installés'}</span>
        {agents.hooksInstalled ? (
          <button type="button" className={SETTINGS_SECONDARY} data-tip={`Retire les hooks Tily de ${agents.settingsFile}, sans toucher au reste`} onClick={onRemoveHooks}>
            Retirer les hooks
          </button>
        ) : (
          <button type="button" className={SETTINGS_PRIMARY} data-tip={`Ajoute les hooks Tily dans ${agents.settingsFile} en gardant ses autres réglages et hooks ; effet aux prochaines sessions claude`} onClick={onInstallHooks}>
            Installer les hooks
          </button>
        )}
      </span>
    </section>
  )
}
