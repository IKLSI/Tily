export enum PreviewKind {
  Markdown = 'markdown',
  Text = 'text',
  Image = 'image',
  Html = 'html',
}

export interface FilePreview {
  path: string
  name: string
  kind: PreviewKind
  language?: string
  content: string
  truncated: boolean
  baseUrl: string
  url?: string
  error?: string
}

export type PreviewHostMessage = { type: 'preview.loaded'; preview: FilePreview; anchor?: string; reload: boolean } | { type: 'preview.requested'; pane: string; path: string }

export type PreviewWebMessage = { type: 'preview.open'; path: string } | { type: 'preview.follow'; path: string; href: string } | { type: 'preview.close' } | { type: 'preview.browser'; path: string }
