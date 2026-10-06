import { app, BrowserWindow, dialog, ipcMain } from 'electron'
import { Backend } from './backend'
import { BrowserPanes } from './browserPanes'
import { pickPath, pickPreferencesToExport, pickPreferencesToImport } from './dialogs'
import { buildMenu } from './menu'
import { isBrowserCall, isFiniteNumber, isNotifyRequest, isRecord, isWebMessage, readPickRequest, readPreviewUrl, readResourceResult, type WebMessage } from './messageGuards'
import { NavigationGuard } from './navigationGuard'
import { bounceDock, Notifier } from './notifications'
import { backendExecutable, preloadScript, startUrl, rendererRoot } from './paths'
import { PreviewResources, registerAppScheme, serveApplication } from './protocols'
import { resolveEnvironment } from './shellEnvironment'

const SEND_CHANNEL = 'tily:send'
const MESSAGE_CHANNEL = 'tily:message'
const SHELL_PREFIX = '{"type":"host.'
const PREVIEW_LOADED_PREFIX = '{"type":"preview.loaded"'
const BACKGROUND = '#17191B'
const RESTART_DELAY_MS = 1_000
const RESTART_WINDOW_MS = 60_000
const MAX_RESTARTS = 3
const ALLOWED_PERMISSIONS = new Set(['clipboard-read', 'clipboard-sanitized-write'])
const WEB_MESSAGE_TYPES = new Set(['app.ready', 'session.save', 'text.save', 'settings.get', 'settings.save', 'appearance.fontSize', 'link.open', 'window.close', 'window.closeCancel'])
const WEB_MESSAGE_PREFIXES = ['attention.', 'agents.', 'mcp.', 'terminal.', 'projects.', 'context.', 'files.', 'preview.', 'git.', 'worktrees.', 'update.', 'statusLog.', 'browser.']

interface ShellMessage {
  type: string
  [key: string]: unknown
}

let window: BrowserWindow | null = null
let backend: Backend | null = null
let quitting = false
let restartTimer: NodeJS.Timeout | null = null
const restartTimes: number[] = []

const canRestart = (): boolean => {
  const now = Date.now()
  const recentRestarts = restartTimes.filter((time) => now - time < RESTART_WINDOW_MS)
  restartTimes.splice(0, restartTimes.length, ...recentRestarts)
  if (restartTimes.length >= MAX_RESTARTS) {
    return false
  }

  restartTimes.push(now)
  return true
}

const cancelRestart = (): void => {
  if (restartTimer) {
    clearTimeout(restartTimer)
    restartTimer = null
  }
}

registerAppScheme()

const sendToWeb = (json: string): void => {
  if (window && !window.isDestroyed()) {
    window.webContents.send(MESSAGE_CHANNEL, json)
  }
}

const parseHostLine = (line: string): unknown => {
  try {
    return JSON.parse(line)
  } catch (error) {
    console.error('Message illisible de l’hôte ignoré :', error)
    return null
  }
}

const showWindow = (): void => {
  if (window && !window.isDestroyed()) {
    if (window.isMinimized()) {
      window.restore()
    }

    window.show()
    window.focus()
  }
}

const quit = (): void => {
  quitting = true
  cancelRestart()
  window?.destroy()
}

const createWindow = (): BrowserWindow =>
  new BrowserWindow({
    width: 1480,
    height: 900,
    show: false,
    backgroundColor: BACKGROUND,
    title: 'Tily',
    webPreferences: { preload: preloadScript(), contextIsolation: true, sandbox: true, spellcheck: false },
  })

const start = async (): Promise<void> => {
  buildMenu()
  const environment = { ...(await resolveEnvironment()), TILY_APP_PID: String(process.pid) }
  const target = createWindow()
  window = target
  const notifier = new Notifier((pane) => {
    showWindow()
    backend?.send({ type: 'host.notificationClicked', pane })
  })
  const resources = new PreviewResources((id, url) => backend?.send({ type: 'host.resource', id, url }))
  const guard = new NavigationGuard(startUrl(), (url) => backend?.send({ type: 'host.previewNavigate', url }))
  const browsers = new BrowserPanes(target, (pane, kind, details) => backend?.send({ type: 'host.browserEvent', pane, kind, ...details }))

  const handleBrowserCall = (message: ShellMessage): void => {
    if (!isBrowserCall(message)) {
      console.error('Appel du navigateur invalide ignoré.')
      if (isFiniteNumber(message.id)) {
        backend?.send({ type: 'host.browserResult', id: message.id, error: 'appel du navigateur invalide' })
      }
      return
    }

    browsers
      .call(message)
      .then((result) => backend?.send({ type: 'host.browserResult', id: message.id, result: result ?? null }))
      .catch((error: unknown) => backend?.send({ type: 'host.browserResult', id: message.id, error: error instanceof Error ? error.message : String(error) }))
  }

  const handleShellMessage = (message: ShellMessage): void => {
    switch (message.type) {
      case 'host.quit':
        browsers.closeAll()
        quit()
        break
      case 'host.notify':
        if (isNotifyRequest(message)) {
          notifier.notify(message)
        } else {
          console.error('Notification de l’hôte invalide ignorée.')
        }
        break
      case 'host.bounce':
        bounceDock()
        break
      case 'host.resourceResult': {
        const answer = readResourceResult(message)
        if (answer) {
          resources.answer(answer.id, answer)
        } else {
          console.error('Réponse de ressource de l’hôte invalide ignorée.')
        }
        break
      }
      case 'host.browser':
        handleBrowserCall(message)
        break
    }
  }

  const handleHostLine = (line: string): void => {
    if (line.startsWith(SHELL_PREFIX)) {
      const message = parseHostLine(line)
      if (isRecord(message) && typeof message.type === 'string') {
        handleShellMessage({ ...message, type: message.type })
      } else {
        console.error('Message de l’hôte sans type ignoré.')
      }
      return
    }

    if (line.startsWith(PREVIEW_LOADED_PREFIX)) {
      const message = parseHostLine(line)
      if (!message) {
        return
      }
      guard.rememberPreview(readPreviewUrl(message))
    }

    sendToWeb(line)
  }

  const restartBackend = (): void => {
    restartTimer = null
    if (quitting || target.isDestroyed()) {
      return
    }

    backend = launchBackend()
    backend.send({ type: 'host.active', active: target.isFocused() })
    target.webContents.reload()
  }

  const handleHostExit = (code: number | null): void => {
    backend = null
    notifier.clear()
    browsers.closeAll()
    if (!quitting && canRestart()) {
      console.error(`L’hôte de Tily s’est arrêté (code ${code ?? 'inconnu'}) : redémarrage.`)
      restartTimer = setTimeout(restartBackend, RESTART_DELAY_MS)
      return
    }

    if (!quitting) {
      dialog.showErrorBox('Tily', `L’hôte de Tily s’est arrêté (code ${code ?? 'inconnu'}).`)
    }

    quitting = true
    app.quit()
  }

  const launchBackend = (): Backend => new Backend(backendExecutable(), environment, handleHostLine, handleHostExit)

  backend = launchBackend()

  const contents = target.webContents
  contents.session.setPermissionRequestHandler((_contents, permission, callback) => callback(ALLOWED_PERMISSIONS.has(permission)))
  contents.session.setPermissionCheckHandler((_contents, permission) => ALLOWED_PERMISSIONS.has(permission))
  serveApplication(contents.session, rendererRoot())
  resources.serve(contents.session)
  guard.attach(contents)

  ipcMain.on(SEND_CHANNEL, (event, message: unknown) => {
    if (event.sender !== contents) {
      return
    }

    if (!isWebMessage(message)) {
      console.error('Message du web sans type refusé.')
      return
    }

    handleWebMessage(target, message)
  })

  target.on('focus', () => backend?.send({ type: 'host.active', active: true }))
  target.on('blur', () => backend?.send({ type: 'host.active', active: false }))
  target.on('close', (event) => {
    if (!quitting && restartTimer) {
      quit()
      return
    }

    if (!quitting) {
      event.preventDefault()
      backend?.send({ type: 'host.closeRequested' })
    }
  })
  target.once('ready-to-show', () => {
    target.maximize()
    target.show()
  })
  await target.loadURL(startUrl())
}

const handleWebMessage = (target: BrowserWindow, message: WebMessage): void => {
  switch (message.type) {
    case 'dialog.pick': {
      const request = readPickRequest(message)
      if (!request) {
        console.error('Demande de sélection de chemin invalide refusée.')
        break
      }

      void pickPath(target, request.target).then((path) => {
        if (path) {
          sendToWeb(JSON.stringify({ type: 'dialog.picked', field: request.field, path }))
        }
      })
      break
    }
    case 'settings.export':
      void pickPreferencesToExport(target).then((path) => path && backend?.send({ type: 'settings.export', path }))
      break
    case 'settings.import':
      void pickPreferencesToImport(target).then((path) => path && backend?.send({ type: 'settings.import', path }))
      break
    default:
      if (WEB_MESSAGE_TYPES.has(message.type) || WEB_MESSAGE_PREFIXES.some((prefix) => message.type.startsWith(prefix))) {
        backend?.send(message)
      } else {
        console.error(`Message du web refusé : ${message.type}`)
      }
  }
}

app.on('before-quit', (event) => {
  if (!quitting && restartTimer) {
    quitting = true
    cancelRestart()
    return
  }

  if (!quitting && window && !window.isDestroyed()) {
    event.preventDefault()
    backend?.send({ type: 'host.closeRequested' })
  }
})

app.on('window-all-closed', () => {
  if (!quitting) {
    return
  }

  if (backend) {
    backend.stop()
  } else {
    app.quit()
  }
})

app.on('activate', showWindow)

app.on('second-instance', showWindow)

if (app.requestSingleInstanceLock()) {
  app.whenReady().then(start).catch((error: Error) => {
    dialog.showErrorBox('Tily', `Tily n’a pas pu démarrer : ${error.message}`)
    app.exit(1)
  })
} else {
  app.quit()
}
