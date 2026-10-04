import type { McpServerInfo } from '../bridge/mcpMessages'
import { installMcpServer, removeMcpServer } from '../mcp/mcpRequests'
import { SETTINGS_BUTTON, SETTINGS_HINT, SETTINGS_SECONDARY } from './settingsStyles'

interface McpSettingsSectionProps {
  sectionClassName: string
  mcp: McpServerInfo
}

const PRIMARY = `${SETTINGS_BUTTON} border-tily-green text-tily-green-deep hover:bg-tily-green-soft`

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
      <h3 className={sectionClassName}>Serveur MCP</h3>
      <p className={SETTINGS_HINT}>Le serveur MCP « tily » permet à Claude Code, lancé dans un pane, de lire la disposition des workspaces, onglets et panes de cette instance de Tily. Il communique par un canal local réservé à votre compte Windows, jamais par le réseau.</p>
      <p className={`${SETTINGS_HINT} font-mono`}>{mcp.executable}</p>
      <p className={SETTINGS_HINT}>{`Le serveur n’est déclaré ou retiré de ${mcp.configFile} (niveau utilisateur de Claude Code) que sur votre clic ; les autres serveurs MCP et réglages de ce fichier sont conservés. Effet aux prochaines sessions claude.`}</p>
      {!mcp.available && <p className="text-[11px] text-tily-warning">{`tily-mcp.exe est introuvable à côté de Tily : ${mcp.executable}`}</p>}
      {otherCopy && <p className="text-[11px] text-tily-warning">{`Déclaré pour une autre copie de Tily : ${mcp.command ?? ''}. « Activer pour cette copie » la remplace.`}</p>}
      <span className="flex items-center gap-3">
        <span className={`text-[11px] ${tone}`}>{text}</span>
        {mcp.installed && (
          <button type="button" className={SETTINGS_SECONDARY} data-tip="Retire le serveur « tily » de la configuration de Claude Code et ferme le canal local" onClick={removeMcpServer}>
            Désactiver
          </button>
        )}
        {(!mcp.installed || otherCopy) && mcp.available && (
          <button type="button" className={PRIMARY} data-tip="Déclare le serveur « tily » dans la configuration utilisateur de Claude Code (fusion, effet aux prochaines sessions claude)" onClick={installMcpServer}>
            {otherCopy ? 'Activer pour cette copie' : 'Activer'}
          </button>
        )}
      </span>
    </section>
  )
}
