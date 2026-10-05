import type { McpServerInfo } from '../../../bridge/mcpMessages'
import { installMcpServer, removeMcpServer } from '../../mcp/mcpRequests'
import { InfoTip } from '../../../components/InfoTip'
import { SETTINGS_PRIMARY, SETTINGS_ROW, SETTINGS_SECONDARY } from './settingsStyles'

interface McpSettingsSectionProps {
  sectionClassName: string
  mcp: McpServerInfo
}

const sameFile = (left: string | undefined, right: string): boolean => left?.toLowerCase() === right.toLowerCase()

const describe = (mcp: McpServerInfo): { text: string; tone: string } => {
  if (!mcp.installed) {
    return { text: 'Serveur MCP désactivé', tone: 'text-tily-warning' }
  }
  if (mcp.error) {
    return { text: `Serveur MCP déclaré, mais ${mcp.error}`, tone: 'text-tily-error' }
  }
  return { text: 'Serveur MCP activé', tone: 'text-tily-green' }
}

export function McpSettingsSection({ sectionClassName, mcp }: McpSettingsSectionProps) {
  const { text, tone } = describe(mcp)
  const otherCopy = mcp.installed && !sameFile(mcp.command, mcp.executable)

  return (
    <section className="flex flex-col gap-2">
      <div className={SETTINGS_ROW}>
        <h3 className={sectionClassName}>Serveur MCP</h3>
        <InfoTip text={`Permet à Claude Code, lancé dans un pane, de lire cette instance de Tily (disposition, texte des terminaux, commandes) et de la piloter : onglets, splits, commandes, worktrees. Agir sur un terminal qu’il n’a pas créé demande votre accord. Canal local réservé à votre compte macOS, jamais le réseau. Exécutable : ${mcp.executable}`} />
      </div>
      {!mcp.available && <p className="text-[11px] text-tily-warning">{`tily-mcp est introuvable à côté de Tily : ${mcp.executable}`}</p>}
      {otherCopy && <p className="text-[11px] text-tily-warning">{`Déclaré pour une autre copie de Tily : ${mcp.command ?? ''}. « Activer pour cette copie » la remplace.`}</p>}
      <span className="flex items-center gap-3">
        <span className={`text-[11px] ${tone}`}>{text}</span>
        {mcp.installed && (
          <button type="button" className={SETTINGS_SECONDARY} data-tip={`Retire le serveur « tily » de ${mcp.configFile} et ferme le canal local`} onClick={removeMcpServer}>
            Désactiver
          </button>
        )}
        {(!mcp.installed || otherCopy) && mcp.available && (
          <button type="button" className={SETTINGS_PRIMARY} data-tip={`Déclare le serveur « tily » dans ${mcp.configFile} (niveau utilisateur de Claude Code) en gardant ses autres serveurs et réglages ; effet aux prochaines sessions claude`} onClick={installMcpServer}>
            {otherCopy ? 'Activer pour cette copie' : 'Activer'}
          </button>
        )}
      </span>
    </section>
  )
}
