import type { WebContents } from 'electron'

const ALLOWED_FRAME_URLS = ['about:blank', 'about:srcdoc']

const documentOf = (url: string): string | null => {
  try {
    const parsed = new URL(url)
    return `${parsed.protocol}//${parsed.host}${parsed.pathname}`
  } catch {
    return null
  }
}

const originOf = (url: string): string | null => {
  try {
    const parsed = new URL(url)
    return `${parsed.protocol}//${parsed.host}`
  } catch {
    return null
  }
}

export class NavigationGuard {
  private previewDocument: string | null = null

  constructor(
    private readonly start: string,
    private readonly followFromPreview: (url: string) => void,
  ) {}

  rememberPreview(url: string | null | undefined): void {
    this.previewDocument = url ? documentOf(url) : null
  }

  attach(contents: WebContents): void {
    contents.on('will-navigate', (event, url) => {
      if (originOf(url) !== originOf(this.start)) {
        event.preventDefault()
      }
    })
    contents.on('will-frame-navigate', (event) => {
      if (event.isMainFrame || ALLOWED_FRAME_URLS.includes(event.url)) {
        return
      }

      if (this.previewDocument === null || documentOf(event.url) !== this.previewDocument) {
        event.preventDefault()
        if (this.previewDocument !== null) {
          this.followFromPreview(event.url)
        }
      }
    })
    contents.setWindowOpenHandler(({ url }) => {
      if (this.previewDocument !== null) {
        this.followFromPreview(url)
      }

      return { action: 'deny' }
    })
  }
}
