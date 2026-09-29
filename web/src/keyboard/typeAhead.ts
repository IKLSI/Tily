import type { KeyboardEvent } from 'react'
import { normalize } from '../palette/searchFilter'

const TYPE_AHEAD_RESET_MS = 700
const SPACE = ' '

export interface TypedText {
  text: string
  at: number
}

export const NO_TYPED_TEXT: TypedText = { text: '', at: 0 }

export const typeAheadText = (previous: TypedText, event: KeyboardEvent): TypedText | null => {
  const altGraph = event.getModifierState('AltGraph')
  if (event.key.length !== 1 || (!altGraph && (event.ctrlKey || event.altKey)) || event.metaKey || event.nativeEvent.isComposing) {
    return null
  }
  const typing = event.timeStamp - previous.at <= TYPE_AHEAD_RESET_MS
  if (event.key === SPACE && !typing) {
    return null
  }
  return { text: typing ? previous.text + event.key : event.key, at: event.timeStamp }
}

export const typeAheadIndex = (names: string[], currentIndex: number, typed: string): number => {
  const cycling = [...typed].every((character) => character === typed[0])
  const wanted = normalize(cycling ? typed[0] : typed)
  const start = cycling ? currentIndex + 1 : Math.max(currentIndex, 0)
  for (let offset = 0; offset < names.length; offset += 1) {
    const index = (start + offset) % names.length
    if (normalize(names[index]).startsWith(wanted)) {
      return index
    }
  }
  return -1
}
