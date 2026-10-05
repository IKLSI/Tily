import { commandTracking, finishedCommandOutput, OutputFailure } from '../terminal/commandOutput'
import { capText, recentText } from '../terminal/terminalText'
import { flagArgument, numberArgument, textArgument, type McpArguments } from './mcpArguments'
import { describeCommand, type McpCommand } from './mcpCommands'
import { locationOf, requireStartedPane } from './mcpPanes'

const DEFAULT_LINES = 100
const MAX_TEXT_CHARS = 40_000
const NO_COMMAND =
  'Aucune commande terminée signalée par ce pane depuis son ouverture (seuls Windows PowerShell et PowerShell 7 signalent leurs commandes) : lisez plutôt ses dernières lignes.'
const TRIMMED = 'La sortie de cette commande n’est plus dans l’historique du terminal : lisez plutôt ses dernières lignes.'

interface McpPaneReading {
  pane: string
  location: string
  shell: string
  path: string
  screen: string
  text: string
  lines: number
  truncated?: true
  command?: McpCommand
}

type PaneContext = Omit<McpPaneReading, 'text' | 'lines' | 'truncated' | 'command'>

const withText = (context: PaneContext, text: string): McpPaneReading => {
  const capped = capText(text, MAX_TEXT_CHARS)
  return capped.truncated ? { ...context, text: capped.text, lines: capped.lines, truncated: true } : { ...context, text: capped.text, lines: capped.lines }
}

export const readPane = (values: McpArguments): McpPaneReading => {
  const target = requireStartedPane(textArgument(values, 'pane'))
  const { terminal } = target.handle
  const context: PaneContext = { pane: target.pane.id, location: locationOf(target), shell: target.pane.shell, path: target.pane.path, screen: terminal.buffer.active.type }
  const commandId = numberArgument(values, 'commandId')
  if (commandId === undefined && !flagArgument(values, 'lastCommand')) {
    return withText(context, recentText(terminal, numberArgument(values, 'lines') ?? DEFAULT_LINES))
  }
  const { finished } = commandTracking(terminal)
  const command = commandId === undefined ? finished.at(-1) : finished.find((candidate) => candidate.id === commandId)
  if (!command) {
    throw new Error(commandId === undefined ? NO_COMMAND : `Commande ${commandId} inconnue dans ce pane : ses identifiants sont donnés par tily_commands, et seules ses 100 dernières commandes sont gardées.`)
  }
  const output = finishedCommandOutput(terminal, command.id)
  if ('failure' in output && output.failure !== OutputFailure.Empty) {
    throw new Error(TRIMMED)
  }
  return { ...withText(context, 'text' in output ? output.text : ''), command: describeCommand(target, command, false) }
}
