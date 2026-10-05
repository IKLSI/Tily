import { execFile } from 'node:child_process'

const MARKER = '__TILY_ENVIRONMENT__'
const TIMEOUT_MS = 5000
const DEFAULT_SHELL = '/bin/zsh'
const PRESERVED_PREFIX = 'TILY_'

const parse = (output: string): Record<string, string> => {
  const start = output.indexOf(MARKER)
  const end = output.lastIndexOf(MARKER)
  if (start < 0 || end <= start) {
    return {}
  }

  const variables: Record<string, string> = {}
  for (const entry of output.slice(start + MARKER.length, end).split('\0')) {
    const separator = entry.indexOf('=')
    if (separator > 0) {
      variables[entry.slice(0, separator)] = entry.slice(separator + 1)
    }
  }

  return variables
}

const loginEnvironment = (): Promise<Record<string, string>> =>
  new Promise((resolve) => {
    const shell = process.env.SHELL || DEFAULT_SHELL
    const command = `printf '%s' '${MARKER}'; /usr/bin/env -0; printf '%s' '${MARKER}'`
    const child = execFile(shell, ['-ilc', command], { timeout: TIMEOUT_MS, maxBuffer: 10 * 1024 * 1024, env: { ...process.env, TILY_RESOLVING_ENVIRONMENT: '1' } }, (_error, stdout) =>
      resolve(parse(stdout ?? '')),
    )
    child.stdin?.end()
  })

export const resolveEnvironment = async (): Promise<NodeJS.ProcessEnv> => {
  const resolved = await loginEnvironment()
  delete resolved.TILY_RESOLVING_ENVIRONMENT
  const preserved = Object.fromEntries(Object.entries(process.env).filter(([key]) => key.startsWith(PRESERVED_PREFIX)))
  return { ...process.env, ...resolved, ...preserved }
}
