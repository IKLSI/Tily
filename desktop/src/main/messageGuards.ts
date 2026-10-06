import type { BrowserCall } from './browserPanes'
import type { NotifyRequest } from './notifications'

export interface ResourceResult {
  id: string
  path: string | null
  contentType: string | null
}

export interface WebMessage {
  type: string
  [key: string]: unknown
}

export interface PickRequest {
  field: string
  target?: string
}

export const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === 'object' && value !== null && !Array.isArray(value)

const isOptionalString = (value: unknown): value is string | null | undefined => value === undefined || value === null || typeof value === 'string'

export const isFiniteNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value)

export const isNotifyRequest = (value: unknown): value is NotifyRequest =>
  isRecord(value) &&
  typeof value.pane === 'string' &&
  Array.isArray(value.lines) &&
  value.lines.every((line) => typeof line === 'string') &&
  isOptionalString(value.sound) &&
  typeof value.bounce === 'boolean' &&
  typeof value.toast === 'boolean'

export const isBrowserCall = (value: unknown): value is BrowserCall =>
  isRecord(value) &&
  isFiniteNumber(value.id) &&
  typeof value.operation === 'string' &&
  typeof value.pane === 'string' &&
  (value.arguments === undefined || value.arguments === null || isRecord(value.arguments))

export const readResourceResult = (value: Record<string, unknown>): ResourceResult | null => {
  const { id, path, contentType } = value
  if ((typeof id !== 'string' && !isFiniteNumber(id)) || !isOptionalString(path) || !isOptionalString(contentType)) {
    return null
  }

  return { id: String(id), path: path ?? null, contentType: contentType ?? null }
}

export const readPreviewUrl = (value: unknown): string | null => {
  if (!isRecord(value) || !isRecord(value.preview)) {
    return null
  }

  return typeof value.preview.url === 'string' ? value.preview.url : null
}

export const isWebMessage = (value: unknown): value is WebMessage => isRecord(value) && typeof value.type === 'string'

export const readPickRequest = (message: WebMessage): PickRequest | null =>
  typeof message.field === 'string' && (message.target === undefined || typeof message.target === 'string') ? { field: message.field, target: message.target } : null
