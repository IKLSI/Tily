import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import { createInterface } from 'node:readline'

const TERMINATE_DELAY_MS = 3_000
const KILL_DELAY_MS = 6_000

type BackendLineListener = (line: string) => void

export class Backend {
  private readonly process: ChildProcessWithoutNullStreams
  private readonly pendingLines: string[] = []
  private readonly stopTimers: NodeJS.Timeout[] = []
  private waitingForDrain = false
  private exited = false

  constructor(executable: string, environment: NodeJS.ProcessEnv, onLine: BackendLineListener, onExit: (code: number | null) => void) {
    this.process = spawn(executable, [], { env: environment, stdio: ['pipe', 'pipe', 'pipe'] })
    const exitOnce = (code: number | null): void => {
      if (this.exited) {
        return
      }

      this.exited = true
      this.pendingLines.length = 0
      this.stopTimers.forEach((timer) => clearTimeout(timer))
      onExit(code)
    }

    createInterface({ input: this.process.stdout, crlfDelay: Infinity }).on('line', (line) => {
      if (line.length > 0) {
        onLine(line)
      }
    })
    this.process.stderr.on('data', (chunk: Buffer) => process.stderr.write(chunk))
    this.process.on('exit', (code) => exitOnce(code))
    this.process.on('error', (error) => {
      process.stderr.write(`Hôte Tily introuvable ou impossible à lancer : ${error.message}\n`)
      exitOnce(null)
    })
    this.process.stdin.on('error', () => undefined)
    this.process.stdin.on('drain', () => this.flushPendingLines())
  }

  send(message: unknown): void {
    this.sendRaw(JSON.stringify(message))
  }

  sendRaw(json: string): void {
    if (!this.process.stdin.writable) {
      return
    }

    const line = `${json}\n`
    if (this.waitingForDrain) {
      this.pendingLines.push(line)
      return
    }

    this.waitingForDrain = !this.process.stdin.write(line)
  }

  stop(): void {
    if (this.exited || this.stopTimers.length > 0) {
      return
    }

    if (this.process.stdin.writable) {
      this.pendingLines.splice(0).forEach((line) => this.process.stdin.write(line))
      this.process.stdin.end()
    }

    this.stopTimers.push(
      setTimeout(() => this.signal('SIGTERM'), TERMINATE_DELAY_MS),
      setTimeout(() => this.signal('SIGKILL'), KILL_DELAY_MS),
    )
  }

  private flushPendingLines(): void {
    this.waitingForDrain = false
    while (!this.waitingForDrain && this.process.stdin.writable) {
      const line = this.pendingLines.shift()
      if (line === undefined) {
        return
      }

      this.waitingForDrain = !this.process.stdin.write(line)
    }
  }

  private signal(name: NodeJS.Signals): void {
    if (this.exited || this.process.exitCode !== null || this.process.signalCode !== null) {
      return
    }

    process.stderr.write(`L’hôte de Tily ne s’est pas arrêté : envoi de ${name}.\n`)
    this.process.kill(name)
  }
}
