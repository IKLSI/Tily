import type { StatusLevel } from '../stores/hostStore'

export interface StatusLogEntry {
  at: string
  level: StatusLevel
  text: string
}

export type StatusLogHostMessage = { type: 'statusLog.added'; entry: StatusLogEntry } | { type: 'statusLog.cleared' }

export type StatusLogWebMessage = { type: 'statusLog.append'; message: string; level: StatusLevel } | { type: 'statusLog.clear' }
