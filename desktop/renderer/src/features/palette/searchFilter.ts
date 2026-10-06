export interface SearchItem {
  id: string
  label: string
  hint?: string
  searchText?: string
  favorite?: boolean
}

interface PreparedText {
  text: string
  words: string[]
}

const LABEL_START_SCORE = 100
const LABEL_WORD_START_SCORE = 80
const LABEL_CONTAINS_SCORE = 60
const WORD_PREFIXES_SCORE = 50
const HINT_WORD_START_SCORE = 40
const HINT_CONTAINS_SCORE = 35
const TYPO_SCORE = 30
const SUBSEQUENCE_SCORE = 20
const NO_MATCH_SCORE = 0

const WORD_PREFIXES_MIN_LENGTH = 2
const SUBSEQUENCE_MIN_LENGTH = 3
const TYPO_MIN_LENGTH = 4
const TWO_TYPOS_MIN_LENGTH = 8

export const normalize = (text: string): string =>
  text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()

const prepare = (text: string): PreparedText => {
  const normalized = normalize(text)
  return { text: normalized, words: normalized.split(/[^a-z0-9]+/).filter((word) => word.length > 0) }
}

const hasWordStartingWith = (words: string[], token: string): boolean => words.some((word) => word.startsWith(token))

const matchesWordPrefixes = (token: string, words: string[]): boolean => {
  const reachable = new Array<boolean>(token.length + 1).fill(false)
  reachable[0] = true
  for (const word of words) {
    for (let start = token.length - 1; start >= 0; start -= 1) {
      if (!reachable[start]) {
        continue
      }
      for (let length = 1; start + length <= token.length && length <= word.length && token[start + length - 1] === word[length - 1]; length += 1) {
        reachable[start + length] = true
      }
    }
    if (reachable[token.length]) {
      return true
    }
  }
  return false
}

const isWordStart = (text: string, index: number): boolean => index === 0 || !/[a-z0-9]/.test(text.charAt(index - 1))

const isSubsequenceFromWordStart = (token: string, text: string): boolean => {
  let start = 0
  while (start < text.length && !(text[start] === token[0] && isWordStart(text, start))) {
    start += 1
  }
  let matched = 1
  for (let index = start + 1; index < text.length; index += 1) {
    if (text[index] === token[matched]) {
      matched += 1
      if (matched === token.length) {
        return true
      }
    }
  }
  return false
}

const distanceAt = (distances: number[], index: number): number => distances[index] ?? Number.POSITIVE_INFINITY

const prefixEditDistance = (token: string, word: string): number => {
  let beforePrevious: number[] = []
  let previous = Array.from({ length: word.length + 1 }, (_, column) => column)
  for (let row = 1; row <= token.length; row += 1) {
    const current = [row]
    for (let column = 1; column <= word.length; column += 1) {
      const substitutionCost = token[row - 1] === word[column - 1] ? 0 : 1
      let distance = Math.min(distanceAt(previous, column) + 1, distanceAt(current, column - 1) + 1, distanceAt(previous, column - 1) + substitutionCost)
      const isTransposition = row > 1 && column > 1 && token[row - 1] === word[column - 2] && token[row - 2] === word[column - 1]
      if (isTransposition) {
        distance = Math.min(distance, distanceAt(beforePrevious, column - 2) + 1)
      }
      current.push(distance)
    }
    beforePrevious = previous
    previous = current
  }
  return Math.min(...previous)
}

const typoScore = (token: string, words: string[]): number => {
  if (token.length < TYPO_MIN_LENGTH) {
    return NO_MATCH_SCORE
  }
  const allowedTypos = token.length >= TWO_TYPOS_MIN_LENGTH ? 2 : 1
  const candidates = words.filter((word) => word[0] === token[0])
  if (candidates.length === 0) {
    return NO_MATCH_SCORE
  }
  const distance = Math.min(...candidates.map((word) => prefixEditDistance(token, word)))
  return distance <= allowedTypos ? TYPO_SCORE - distance : NO_MATCH_SCORE
}

const tokenScore = (token: string, label: PreparedText, hint: PreparedText): number => {
  if (label.text.startsWith(token)) {
    return LABEL_START_SCORE
  }
  if (hasWordStartingWith(label.words, token)) {
    return LABEL_WORD_START_SCORE
  }
  if (label.text.includes(token)) {
    return LABEL_CONTAINS_SCORE
  }
  if (token.length >= WORD_PREFIXES_MIN_LENGTH && matchesWordPrefixes(token, label.words)) {
    return WORD_PREFIXES_SCORE
  }
  if (hasWordStartingWith(hint.words, token)) {
    return HINT_WORD_START_SCORE
  }
  if (hint.text.includes(token)) {
    return HINT_CONTAINS_SCORE
  }
  const score = typoScore(token, label.words)
  if (score !== NO_MATCH_SCORE) {
    return score
  }
  return token.length >= SUBSEQUENCE_MIN_LENGTH && isSubsequenceFromWordStart(token, label.text) ? SUBSEQUENCE_SCORE : NO_MATCH_SCORE
}

const itemScore = (item: SearchItem, tokens: string[]): number => {
  const label = prepare(item.label)
  const hint = prepare(item.hint ?? '')
  const searchText = item.searchText === undefined ? null : normalize(item.searchText)
  let total = 0
  for (const token of tokens) {
    const score = Math.max(tokenScore(token, label, hint), searchText?.includes(token) ? HINT_CONTAINS_SCORE : NO_MATCH_SCORE)
    if (score === NO_MATCH_SCORE) {
      return NO_MATCH_SCORE
    }
    total += score
  }
  return total
}

export const filterSearchItems = <T extends SearchItem>(items: T[], query: string): T[] => {
  const tokens = normalize(query).split(/\s+/).filter((token) => token.length > 0)
  if (tokens.length === 0) {
    return items
  }
  return items
    .map((item) => ({ item, score: itemScore(item, tokens) }))
    .filter(({ score }) => score > NO_MATCH_SCORE)
    .sort((first, second) => second.score - first.score)
    .map(({ item }) => item)
}
