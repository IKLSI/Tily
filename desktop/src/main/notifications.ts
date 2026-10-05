import { app, Notification } from 'electron'
import { spawn } from 'node:child_process'
import path from 'node:path'

const SYSTEM_SOUNDS = '/System/Library/Sounds'
const SOUND_EXTENSION = '.aiff'
const PLAYER = '/usr/bin/afplay'

export interface NotifyRequest {
  pane: string
  lines: string[]
  sound?: string | null
  bounce: boolean
  toast: boolean
}

const soundFile = (sound: string): string => (path.isAbsolute(sound) ? sound : path.join(SYSTEM_SOUNDS, `${sound}${SOUND_EXTENSION}`))

const playSound = (sound: string): void => {
  const player = spawn(PLAYER, [soundFile(sound)], { stdio: 'ignore' })
  player.on('error', () => undefined)
}

export const bounceDock = (): void => {
  app.dock?.bounce('informational')
}

export class Notifier {
  private readonly shown = new Map<string, Notification>()

  constructor(private readonly onClick: (pane: string) => void) {}

  notify(request: NotifyRequest): void {
    if (request.bounce) {
      bounceDock()
    }

    if (request.sound) {
      playSound(request.sound)
    }

    if (!request.toast || !Notification.isSupported() || request.lines.length === 0) {
      return
    }

    this.shown.get(request.pane)?.close()
    const [title, ...body] = request.lines
    const notification = new Notification({ title, body: body.join('\n'), silent: true })
    notification.on('click', () => {
      this.shown.delete(request.pane)
      this.onClick(request.pane)
    })
    this.shown.set(request.pane, notification)
    notification.show()
  }

  clear(): void {
    this.shown.forEach((notification) => notification.close())
    this.shown.clear()
  }
}
