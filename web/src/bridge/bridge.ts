import type { HostMessageOf, HostMessageType, HostToWebMessage, WebToHostMessage } from './messages'

type Handler<T extends HostMessageType> = (message: HostMessageOf<T>) => void

interface WebViewChannel {
  postMessage(message: unknown): void
  postMessageWithAdditionalObjects?(message: unknown, additionalObjects: ArrayLike<File>): void
  addEventListener(type: 'message', listener: (event: { data: HostToWebMessage }) => void): void
}

const channel = (window as unknown as { chrome?: { webview?: WebViewChannel } }).chrome?.webview
const handlers = new Map<HostMessageType, Set<Handler<HostMessageType>>>()

const dispatch = (message: HostToWebMessage): void => {
  handlers.get(message.type)?.forEach((handler) => handler(message))
}

channel?.addEventListener('message', (event) => dispatch(event.data))

export const bridge = {
  available: Boolean(channel),
  send(message: WebToHostMessage): void {
    channel?.postMessage(message)
  },
  sendWithFiles(message: WebToHostMessage, files: ArrayLike<File>): void {
    channel?.postMessageWithAdditionalObjects?.(message, files)
  },
  on<T extends HostMessageType>(type: T, handler: Handler<T>): () => void {
    const set = handlers.get(type) ?? new Set<Handler<HostMessageType>>()
    set.add(handler as unknown as Handler<HostMessageType>)
    handlers.set(type, set)
    return () => {
      set.delete(handler as unknown as Handler<HostMessageType>)
    }
  },
}
