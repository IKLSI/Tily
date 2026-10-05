import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import { createInterface } from 'node:readline'

type BackendLineListener = (line: string) => void

export class Backend {
  private readonly process: ChildProcessWithoutNullStreams

  constructor(executable: string, environment: NodeJS.ProcessEnv, onLine: BackendLineListener, onExit: (code: number | null) => void) {
    this.process = spawn(executable, [], { env: environment, stdio: ['pipe', 'pipe', 'pipe'] })
    createInterface({ input: this.process.stdout, crlfDelay: Infinity }).on('line', (line) => {
      if (line.length > 0) {
        onLine(line)
      }
    })
    this.process.stderr.on('data', (chunk: Buffer) => process.stderr.write(chunk))
    this.process.on('exit', onExit)
    this.process.on('error', (error) => {
      process.stderr.write(`Hôte Tily introuvable ou impossible à lancer : ${error.message}\n`)
      onExit(null)
    })
    this.process.stdin.on('error', () => undefined)
  }

  send(message: unknown): void {
    this.sendRaw(JSON.stringify(message))
  }

  sendRaw(json: string): void {
    if (this.process.stdin.writable) {
      this.process.stdin.write(`${json}\n`)
    }
  }

  stop(): void {
    this.process.stdin.end()
  }
}
