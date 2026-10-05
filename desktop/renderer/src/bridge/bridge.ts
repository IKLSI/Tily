import type { HostMessageOf, HostMessageType, HostToWebMessage, WebToHostMessage } from './messages'

type Handler<T extends HostMessageType> = (message: HostMessageOf<T>) => void

interface TilyChannel {
  send(message: unknown): void
  onMessage(listener: (message: HostToWebMessage) => void): void
  pathForFile(file: File): string
}

const channel = (window as unknown as { tily?: TilyChannel }).tily
const handlers = new Map<HostMessageType, Set<Handler<HostMessageType>>>()

const dispatch = (message: HostToWebMessage): void => {
  handlers.get(message.type)?.forEach((handler) => handler(message))
}

channel?.onMessage(dispatch)

export const bridge = {
  available: Boolean(channel),
  send(message: WebToHostMessage): void {
    channel?.send(message)
  },
  pathsOf(files: ArrayLike<File>): string[] {
    return channel ? Array.from(files, (file) => channel.pathForFile(file)).filter((path) => path.length > 0) : []
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
