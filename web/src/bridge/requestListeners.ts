const FIRST_AGENT_REQUEST = 1_000_000

export const CANCELLED_REPLY = 'cancelled'

type ReplyListener = (type: string, message: unknown) => void

const listeners = new Map<number, ReplyListener>()
let nextRequest = FIRST_AGENT_REQUEST

export const newAgentRequest = (): number => nextRequest++

export const listenReplies = (request: number, listener: ReplyListener): (() => void) => {
  listeners.set(request, listener)
  return () => {
    listeners.delete(request)
  }
}

export const dispatchReply = (type: string, request: number | undefined, message: unknown): boolean => {
  const listener = request === undefined ? undefined : listeners.get(request)
  listener?.(type, message)
  return listener !== undefined
}
