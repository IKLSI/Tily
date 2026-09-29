export enum PreviewKind {
  Markdown = 'markdown',
  Text = 'text',
}

export interface FilePreview {
  path: string
  name: string
  kind: PreviewKind
  language?: string
  content: string
  truncated: boolean
  baseUrl: string
  error?: string
}

export type PreviewHostMessage = { type: 'preview.loaded'; preview: FilePreview; anchor?: string; reload: boolean }

export type PreviewWebMessage = { type: 'preview.open'; path: string } | { type: 'preview.follow'; path: string; href: string } | { type: 'preview.close' }
