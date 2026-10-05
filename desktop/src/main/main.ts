import { app, BrowserWindow, dialog, ipcMain } from 'electron'
import { Backend } from './backend'
import { BrowserPanes, type BrowserCall } from './browserPanes'
import { pickPath, pickPreferencesToExport, pickPreferencesToImport } from './dialogs'
import { buildMenu } from './menu'
import { NavigationGuard } from './navigationGuard'
import { bounceDock, Notifier, type NotifyRequest } from './notifications'
import { backendExecutable, preloadScript, startUrl, rendererRoot } from './paths'
import { PreviewResources, registerAppScheme, serveApplication } from './protocols'
import { resolveEnvironment } from './shellEnvironment'

const SEND_CHANNEL = 'tily:send'
const MESSAGE_CHANNEL = 'tily:message'
const SHELL_PREFIX = '{"type":"host.'
const PREVIEW_LOADED_PREFIX = '{"type":"preview.loaded"'
const BACKGROUND = '#17191B'
const ALLOWED_PERMISSIONS = new Set(['clipboard-read', 'clipboard-sanitized-write'])

interface WebMessage {
  type: string
  field?: string
  target?: string
}

interface ShellMessage {
  type: string
  [key: string]: unknown
}

let window: BrowserWindow | null = null
let backend: Backend | null = null
let quitting = false

registerAppScheme()

const sendToWeb = (json: string): void => {
  if (window && !window.isDestroyed()) {
    window.webContents.send(MESSAGE_CHANNEL, json)
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

  const handleShellMessage = (message: ShellMessage): void => {
    switch (message.type) {
      case 'host.quit':
        browsers.closeAll()
        quit()
        break
      case 'host.notify':
        notifier.notify(message as unknown as NotifyRequest)
        break
      case 'host.bounce':
        bounceDock()
        break
      case 'host.resourceResult':
        resources.answer(String(message.id), { path: message.path as string | null, contentType: message.contentType as string | null })
        break
      case 'host.browser':
        browsers
          .call(message as unknown as BrowserCall)
          .then((result) => backend?.send({ type: 'host.browserResult', id: message.id, result: result ?? null }))
          .catch((error: Error) => backend?.send({ type: 'host.browserResult', id: message.id, error: error.message }))
        break
    }
  }

  backend = new Backend(
    backendExecutable(),
    environment,
    (line) => {
      if (line.startsWith(SHELL_PREFIX)) {
        handleShellMessage(JSON.parse(line) as ShellMessage)
        return
      }

      if (line.startsWith(PREVIEW_LOADED_PREFIX)) {
        guard.rememberPreview((JSON.parse(line) as { preview?: { url?: string } }).preview?.url)
      }

      sendToWeb(line)
    },
    (code) => {
      notifier.clear()
      browsers.closeAll()
      if (!quitting) {
        dialog.showErrorBox('Tily', `L’hôte de Tily s’est arrêté (code ${code ?? 'inconnu'}).`)
      }

      quitting = true
      app.quit()
    },
  )

  const contents = target.webContents
  contents.session.setPermissionRequestHandler((_contents, permission, callback) => callback(ALLOWED_PERMISSIONS.has(permission)))
  contents.session.setPermissionCheckHandler((_contents, permission) => ALLOWED_PERMISSIONS.has(permission))
  serveApplication(contents.session, rendererRoot())
  resources.serve(contents.session)
  guard.attach(contents)

  ipcMain.on(SEND_CHANNEL, (event, message: WebMessage) => {
    if (event.sender !== contents) {
      return
    }

    handleWebMessage(target, message)
  })

  target.on('focus', () => backend?.send({ type: 'host.active', active: true }))
  target.on('blur', () => backend?.send({ type: 'host.active', active: false }))
  target.on('close', (event) => {
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
    case 'dialog.pick':
      void pickPath(target, message.target).then((path) => {
        if (path) {
          sendToWeb(JSON.stringify({ type: 'dialog.picked', field: message.field, path }))
        }
      })
      break
    case 'settings.export':
      void pickPreferencesToExport(target).then((path) => path && backend?.send({ type: 'settings.export', path }))
      break
    case 'settings.import':
      void pickPreferencesToImport(target).then((path) => path && backend?.send({ type: 'settings.import', path }))
      break
    default:
      backend?.send(message)
  }
}

app.on('before-quit', (event) => {
  if (!quitting && window && !window.isDestroyed()) {
    event.preventDefault()
    backend?.send({ type: 'host.closeRequested' })
  }
})

app.on('window-all-closed', () => {
  if (quitting) {
    backend?.stop()
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
