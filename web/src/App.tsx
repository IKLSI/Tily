import { useEffect } from 'react'
import { startAttentionNotifier } from './agents/attentionNotifier'
import { bridge } from './bridge/bridge'
import { AppShell } from './components/AppShell'
import { allPanes } from './model/session'
import { useAgentStore } from './store/agentStore'
import { StatusLevel, useHostStore } from './store/hostStore'
import { usePaneStore } from './store/paneStore'
import { useSessionStore } from './store/sessionStore'
import { useUiStore } from './store/uiStore'
import { receiveActivity, receiveApplicationClosing } from './terminal/closeGuard'
import { receiveContext } from './terminal/contextActions'
import { receiveCreated, receiveDeleted, receiveListing, receiveRenamed } from './explorer/fileExplorerActions'
import { receiveGitChanged, receiveGitDetails, receiveGitDiff, receiveGitDone, receiveGitFailed, receiveGitHistory, receiveGitPushRejected, receiveGitState } from './git/gitReceivers'
import { terminalRegistry } from './terminal/terminalRegistry'
import { forgetRemovedText, markTextSaveFailed, primeSessionText, startTextAutosave } from './terminal/textPersistence'

const SAVE_DEBOUNCE_MS = 500

export default function App() {
  const session = useSessionStore((state) => state.session)
  const connected = useHostStore((state) => state.connected)
  const status = useHostStore((state) => state.status)

  useEffect(() => {
    const { load, setPanePath } = useSessionStore.getState()
    const { setHello, setStatus, setProjects, setUnsaved, applySettings, setPickedPath, setImportedPreferences } = useHostStore.getState()
    let stopAutosave: (() => void) | undefined
    const stopNotifier = startAttentionNotifier()
    const { markFailed, markExited, markPathMissing, clear } = usePaneStore.getState()
    const subscriptions = [
      bridge.on('app.hello', (message) => {
        setHello(message.shells, message.home, message.persistence)
        terminalRegistry.configure(message.persistence.linesPerPane)
        primeSessionText(message.session, message.text)
        void document.fonts.load('14px "Symbols Nerd Font Mono"').then(() => {
          load(message.session)
          stopAutosave?.()
          stopAutosave = startTextAutosave(message.persistence.textIntervalSeconds)
          if (message.recovery) {
            setStatus(message.recovery, StatusLevel.Warning)
          } else {
            setStatus('Session restaurée : nouveaux shells, aucune commande rejouée.')
          }
        })
      }),
      bridge.on('settings.result', (message) => {
        applySettings({ settings: message.settings, shellSettings: message.shellSettings, files: message.files, warnings: message.warnings, agents: message.agents, notifications: message.notifications }, message.shells, message.persistence)
        if (!message.saved) {
          return
        }
        terminalRegistry.configure(message.persistence.linesPerPane)
        stopAutosave?.()
        stopAutosave = startTextAutosave(message.persistence.textIntervalSeconds)
        useUiStore.getState().closeSettings()
        const warnings = message.warnings.join(' ')
        setStatus(warnings.length > 0 ? `Réglages enregistrés. ${warnings}` : 'Réglages enregistrés et appliqués.', warnings.length > 0 ? StatusLevel.Warning : StatusLevel.Info)
      }),
      bridge.on('dialog.picked', (message) => setPickedPath({ field: message.field, path: message.path })),
      bridge.on('settings.exported', (message) => setStatus(`Préférences exportées dans ${message.path}.`)),
      bridge.on('settings.imported', (message) => {
        setImportedPreferences({ settings: message.settings, path: message.path })
        setStatus(`Préférences lues depuis ${message.path} : Enregistrer remplace la configuration actuelle.`)
      }),
      bridge.on('app.closing', (message) => receiveApplicationClosing(message.activity)),
      bridge.on('terminal.activityResult', (message) => receiveActivity(message.panes)),
      bridge.on('agent.states', (message) => useAgentStore.getState().setAgents(message.panes)),
      bridge.on('agent.join', (message) => {
        useAgentStore.getState().acknowledge(message.pane)
        useSessionStore.getState().selectPane(message.pane)
        terminalRegistry.get(message.pane)?.terminal.focus()
      }),
      bridge.on('session.saved', () => setUnsaved(false)),
      bridge.on('session.saveFailed', (message) => {
        setUnsaved(true)
        markTextSaveFailed()
        setStatus(`${message.message} Les changements ne sont pas enregistrés.`, StatusLevel.Error)
      }),
      bridge.on('terminal.output', (message) => terminalRegistry.write(message.pane, message.data)),
      bridge.on('terminal.cwd', (message) => {
        setPanePath(message.pane, message.path)
        clear(message.pane)
      }),
      bridge.on('terminal.pathMissing', (message) => markPathMissing(message.pane, message.path, message.fallback)),
      bridge.on('projects.listed', (message) => setProjects(message.root, message.projects, message.error ?? null)),
      bridge.on('context.result', (message) => receiveContext(message.pane, message.path, message.git)),
      bridge.on('files.listed', (message) => receiveListing(message.path, message.entries, message.total, message.error)),
      bridge.on('files.created', (message) => receiveCreated(message.path)),
      bridge.on('files.renamed', (message) => receiveRenamed(message.path, message.target)),
      bridge.on('files.deleted', (message) => receiveDeleted(message.path)),
      bridge.on('git.state', (message) => receiveGitState(message.path, message.state, message.error)),
      bridge.on('git.changed', (message) => receiveGitChanged(message.path)),
      bridge.on('git.history', (message) => receiveGitHistory(message.history, message.error)),
      bridge.on('git.diff', (message) => receiveGitDiff(message.request, message.result, message.error)),
      bridge.on('git.details', (message) => receiveGitDetails(message.request, message.result, message.error)),
      bridge.on('git.done', (message) => receiveGitDone(message.operation, message.message, message.warning)),
      bridge.on('git.failed', (message) => receiveGitFailed(message.operation, message.message, message.output, message.code)),
      bridge.on('git.pushRejected', (message) => receiveGitPushRejected(message.operation, message.branch, message.message, message.output)),
      bridge.on('terminal.exit', (message) => {
        terminalRegistry.markExited(message.pane, message.code)
        markExited(message.pane, message.code)
      }),
      bridge.on('error', (message) => {
        setStatus(message.message, StatusLevel.Error)
        if (message.pane) {
          markFailed(message.pane, message.message)
        }
      }),
    ]
    if (bridge.available) {
      bridge.send({ type: 'app.ready' })
    } else {
      setStatus('Cette page doit être ouverte dans l’hôte Dock : aucun pont détecté.', StatusLevel.Error)
    }
    return () => {
      stopNotifier()
      stopAutosave?.()
      subscriptions.forEach((unsubscribe) => unsubscribe())
    }
  }, [])

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | undefined
    return useSessionStore.subscribe((state, previous) => {
      if (!state.session || state.session === previous.session) {
        return
      }
      const removed = terminalRegistry.disposeMissing(new Set(allPanes(state.session).map((pane) => pane.id)))
      clearTimeout(timer)
      timer = setTimeout(() => {
        bridge.send({ type: 'session.save', session: state.session! })
        if (removed) {
          forgetRemovedText()
        }
      }, SAVE_DEBOUNCE_MS)
    })
  }, [])

  if (!session || !connected) {
    return <div className="flex h-full items-center justify-center text-dock-muted">{status.text}</div>
  }

  return <AppShell session={session} />
}
