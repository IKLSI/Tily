import { WebContentsView, type BrowserWindow, type WebContents } from 'electron'

const PARTITION = 'persist:browser'
const PROTOCOL_VERSION = '1.3'
const KEY_BINDING = '__tilyKey'
const ABORTED = -3
const SNAPSHOT_QUALITY = 75
const BACKGROUND = '#ffffff'
const BLANK = 'about:blank'
const POPUP_FEATURES = /(^|,)\s*(width|height|left|top)\s*=/i

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
  ) {}

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
        view.setBounds({ x: Math.round(Number(args.x)), y: Math.round(Number(args.y)), width: Math.round(Number(args.width)), height: Math.round(Number(args.height)) })
        view.setVisible(Boolean(args.visible))
        return null
      case 'navigate':
        contents.loadURL(String(args.url)).catch(() => undefined)
        return null
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
        return contents.debugger.sendCommand(String(args.method), (args.params as object | undefined) ?? {})
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
    contents.setWindowOpenHandler(({ url, features }) => {
      if (POPUP_FEATURES.test(features)) {
        return { action: 'allow' }
      }

      this.emit(pane, 'newPane', { url })
      return { action: 'deny' }
    })
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
