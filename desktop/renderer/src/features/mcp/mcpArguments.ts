export type McpArguments = Record<string, unknown>

export const argumentsOf = (value: unknown): McpArguments => (typeof value === 'object' && value !== null && !Array.isArray(value) ? (value as McpArguments) : {})

export const textArgument = (values: McpArguments, name: string): string | undefined => {
  const value = values[name]
  return typeof value === 'string' && value !== '' ? value : undefined
}

export const numberArgument = (values: McpArguments, name: string): number | undefined => {
  const value = values[name]
  return typeof value === 'number' && Number.isFinite(value) ? Math.trunc(value) : undefined
}

export const flagArgument = (values: McpArguments, name: string): boolean => values[name] === true
