import { session, WebContentsView, type BrowserWindow, type Rectangle, type WebContents } from 'electron'
import { isFiniteNumber, isRecord } from './messageGuards'

const PARTITION = 'persist:browser'
const PROTOCOL_VERSION = '1.3'
const KEY_BINDING = '__tilyKey'
const ABORTED = -3
const SNAPSHOT_QUALITY = 75
const BACKGROUND = '#ffffff'
const BLANK = 'about:blank'
const POPUP_FEATURES = /(^|,)\s*(width|height|left|top)\s*=/i
const ALLOWED_PROTOCOLS = new Set(['http:', 'https:', 'file:'])
const ALLOWED_PERMISSIONS = new Set(['clipboard-sanitized-write'])

const isAllowedAddress = (url: string): boolean => {
  if (url === BLANK) {
    return true
  }

  try {
    return ALLOWED_PROTOCOLS.has(new URL(url).protocol)
  } catch {
    return false
  }
}

const readBounds = (args: Record<string, unknown>): Rectangle => {
  const { x, y, width, height } = args
  if (!isFiniteNumber(x) || !isFiniteNumber(y) || !isFiniteNumber(width) || !isFiniteNumber(height)) {
    throw new Error('dimensions de la vue invalides')
  }

  return { x: Math.round(x), y: Math.round(y), width: Math.round(width), height: Math.round(height) }
}

const readText = (args: Record<string, unknown>, name: string, label: string): string => {
  const value = args[name]
  if (typeof value !== 'string') {
    throw new Error(`${label} invalide`)
  }

  return value
}

export interface BrowserCall {
  id: number
  operation: string
  pane: string
  arguments?: Record<string, unknown> | null
}

type BrowserEmit = (pane: string, kind: string, details?: Record<string, unknown>) => void

export class BrowserPanes {
  private readonly views = new Map<string, WebContentsView>()

  constructor(
    private readonly window: BrowserWindow,
    private readonly emit: BrowserEmit,
  ) {
    const browserSession = session.fromPartition(PARTITION)
    browserSession.setPermissionRequestHandler((_contents, permission, callback) => callback(ALLOWED_PERMISSIONS.has(permission)))
    browserSession.setPermissionCheckHandler((_contents, permission) => ALLOWED_PERMISSIONS.has(permission))
  }

  async call(request: BrowserCall): Promise<unknown> {
    const args = request.arguments ?? {}
    if (request.operation === 'create') {
      await this.create(request.pane)
      return null
    }

    const view = this.views.get(request.pane)
    if (!view) {
      throw new Error('vue introuvable')
    }

    const contents = view.webContents
    switch (request.operation) {
      case 'layout':
        view.setBounds(readBounds(args))
        view.setVisible(Boolean(args.visible))
        return null
      case 'navigate': {
        const url = readText(args, 'url', 'adresse')
        if (!isAllowedAddress(url)) {
          throw new Error(`adresse non autorisée : ${url}`)
        }

        contents.loadURL(url).catch(() => undefined)
        return null
      }
      case 'back':
        if (contents.navigationHistory.canGoBack()) {
          contents.navigationHistory.goBack()
        }
        return null
      case 'reload':
        contents.reload()
        return null
      case 'devtools':
        contents.openDevTools({ mode: 'detach' })
        return null
      case 'focus':
        contents.focus()
        return null
      case 'close':
        this.close(request.pane)
        return null
      case 'cdp':
        return contents.debugger.sendCommand(readText(args, 'method', 'méthode CDP'), isRecord(args.params) ? args.params : {})
      case 'snapshot':
        return `data:image/jpeg;base64,${(await contents.capturePage()).toJPEG(SNAPSHOT_QUALITY).toString('base64')}`
      default:
        throw new Error(`opération inconnue : ${request.operation}`)
    }
  }

  closeAll(): void {
    Array.from(this.views.keys()).forEach((pane) => this.close(pane))
  }

  private async create(pane: string): Promise<void> {
    if (this.views.has(pane)) {
      return
    }

    const view = new WebContentsView({ webPreferences: { partition: PARTITION, sandbox: true, contextIsolation: true } })
    view.setBackgroundColor(BACKGROUND)
    view.setVisible(false)
    this.window.contentView.addChildView(view)
    this.views.set(pane, view)
    this.observe(pane, view.webContents)
    view.webContents.debugger.attach(PROTOCOL_VERSION)
    await view.webContents.loadURL(BLANK).catch(() => undefined)
  }

  private observe(pane: string, contents: WebContents): void {
    const state = (): void =>
      this.emit(pane, 'state', {
        url: contents.getURL(),
        title: contents.getTitle(),
        canGoBack: contents.navigationHistory.canGoBack(),
        canGoForward: contents.navigationHistory.canGoForward(),
      })

    contents.on('did-start-navigation', (event) => {
      if (event.isMainFrame && !event.isSameDocument) {
        this.emit(pane, 'started')
      }
    })
    contents.on('did-navigate', (_event, _url, status) => {
      this.emit(pane, 'navigated', { status })
      state()
    })
    contents.on('did-navigate-in-page', state)
    contents.on('page-title-updated', state)
    contents.on('did-finish-load', () => {
      state()
      this.emit(pane, 'finished', { success: true })
    })
    contents.on('did-fail-load', (_event, code, description, _url, isMainFrame) => {
      if (isMainFrame && code !== ABORTED) {
        state()
        this.emit(pane, 'finished', { success: false, error: description })
      }
    })
    contents.on('focus', () => this.emit(pane, 'focused'))
    contents.debugger.on('message', (_event, method, params) => {
      if (method === 'Runtime.bindingCalled' && params?.name === KEY_BINDING) {
        this.window.webContents.focus()
      }

      this.emit(pane, 'cdp', { method, params })
    })
    this.guard(pane, contents)
  }

  private guard(pane: string, contents: WebContents): void {
    contents.on('will-navigate', (event) => {
      if (!isAllowedAddress(event.url)) {
        event.preventDefault()
      }
    })
    contents.on('will-redirect', (event) => {
      if (!isAllowedAddress(event.url)) {
        event.preventDefault()
      }
    })
    contents.setWindowOpenHandler(({ url, features }) => {
      if (!isAllowedAddress(url)) {
        return { action: 'deny' }
      }

      if (POPUP_FEATURES.test(features)) {
        return { action: 'allow' }
      }

      this.emit(pane, 'newPane', { url })
      return { action: 'deny' }
    })
    contents.on('did-create-window', (popup) => this.guard(pane, popup.webContents))
  }

  private close(pane: string): void {
    const view = this.views.get(pane)
    if (!view) {
      return
    }

    this.views.delete(pane)
    if (!this.window.isDestroyed()) {
      this.window.contentView.removeChildView(view)
    }

    if (view.webContents.isDestroyed()) {
      return
    }

    if (view.webContents.debugger.isAttached()) {
      view.webContents.debugger.detach()
    }

    view.webContents.close()
  }
}
