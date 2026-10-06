import { net, protocol, type Session } from 'electron'
import { readFile } from 'node:fs/promises'
import path from 'node:path'
import { pathToFileURL } from 'node:url'
import { APP_HOST, APP_SCHEME } from './paths'

const PREVIEW_HOST = 'tily.files'
const RESOURCE_TIMEOUT_MS = 10_000
const HTML_EXTENSION = '.html'
const CONTENT_SECURITY_POLICY = [
  "default-src 'self'",
  "script-src 'self'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob: https: http:",
  "font-src 'self' data:",
  "connect-src 'self'",
  `frame-src https://${PREVIEW_HOST}`,
  "object-src 'none'",
  "base-uri 'none'",
  "frame-ancestors 'none'",
].join('; ')

interface ResourceAnswer {
  path?: string | null
  contentType?: string | null
}

type ResourceAsker = (id: string, url: string) => void

const notFound = (): Response => new Response('Introuvable', { status: 404 })

export const registerAppScheme = (): void => {
  protocol.registerSchemesAsPrivileged([{ scheme: APP_SCHEME, privileges: { standard: true, secure: true, supportFetchAPI: true } }])
}

export const serveApplication = (session: Session, root: string): void => {
  session.protocol.handle(APP_SCHEME, async (request) => {
    const { host, pathname } = new URL(request.url)
    if (host !== APP_HOST) {
      return notFound()
    }

    const target = path.resolve(root, `.${decodeURIComponent(pathname)}`)
    const relative = path.relative(root, target)
    if (!relative || relative === '..' || relative.startsWith(`..${path.sep}`) || path.isAbsolute(relative)) {
      return notFound()
    }

    const response = await net.fetch(pathToFileURL(target).toString())
    if (path.extname(target) !== HTML_EXTENSION) {
      return response
    }

    const headers = new Headers(response.headers)
    headers.set('content-security-policy', CONTENT_SECURITY_POLICY)
    return new Response(response.body, { status: response.status, statusText: response.statusText, headers })
  })
}

export class PreviewResources {
  private readonly pending = new Map<string, (answer: ResourceAnswer) => void>()
  private nextId = 0

  constructor(private readonly ask: ResourceAsker) {}

  serve(session: Session): void {
    session.protocol.handle('https', async (request) => {
      if (new URL(request.url).host !== PREVIEW_HOST) {
        return net.fetch(request, { bypassCustomProtocolHandlers: true })
      }

      const answer = await this.resolve(request.url)
      if (!answer.path || !answer.contentType) {
        return notFound()
      }

      try {
        const content = await readFile(answer.path)
        return new Response(content, { headers: { 'content-type': answer.contentType, 'cache-control': 'no-cache' } })
      } catch {
        return notFound()
      }
    })
  }

  answer(id: string, answer: ResourceAnswer): void {
    this.pending.get(id)?.(answer)
  }

  private resolve(url: string): Promise<ResourceAnswer> {
    const id = String(++this.nextId)
    return new Promise((resolve) => {
      const timer = setTimeout(() => finish({}), RESOURCE_TIMEOUT_MS)
      const finish = (answer: ResourceAnswer): void => {
        clearTimeout(timer)
        this.pending.delete(id)
        resolve(answer)
      }
      this.pending.set(id, finish)
      this.ask(id, url)
    })
  }
}
