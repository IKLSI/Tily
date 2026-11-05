import { highlightLine, isHighlightable } from '../preview/renderPreview'

const MAX_LINE_LENGTH = 2000
const MAX_CACHED_LINES = 5000
const FILE_NAMES: Record<string, string> = { dockerfile: 'dockerfile', makefile: 'makefile' }
const EXTENSIONS: Record<string, string> = { slnx: 'xml', csproj: 'xml', props: 'xml', targets: 'xml', plist: 'xml', mjs: 'javascript', cjs: 'javascript', zsh: 'bash', env: 'ini' }

const cache = new Map<string, string | null>()

export const diffLanguageOf = (path: string): string | null => {
  const name = (path.split('/').pop() ?? '').toLowerCase()
  const dot = name.lastIndexOf('.')
  const extension = dot > 0 ? name.slice(dot + 1) : ''
  const candidates = [FILE_NAMES[name], EXTENSIONS[extension], extension]
  return candidates.find((candidate) => candidate !== undefined && candidate.length > 0 && isHighlightable(candidate)) ?? null
}

export const highlightDiffLine = (language: string | null, text: string): string | null => {
  if (!language || text.length === 0 || text.length > MAX_LINE_LENGTH) {
    return null
  }
  const key = `${language}\n${text}`
  const cached = cache.get(key)
  if (cached !== undefined) {
    return cached
  }
  if (cache.size >= MAX_CACHED_LINES) {
    cache.clear()
  }
  const html = highlightLine(text, language)
  cache.set(key, html)
  return html
}
