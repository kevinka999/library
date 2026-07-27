import type {
  BookHistoryFilters,
  HistorySortDirection,
  KnownChangedField,
} from '../../api/books/get-book-history'

export const knownChangedFields: KnownChangedField[] = [
  'title',
  'shortDescription',
  'publishDate',
  'authors',
]

export type ParsedHistoryFilters =
  | { valid: true; value: BookHistoryFilters }
  | { valid: false; value: BookHistoryFilters }

function parseInstant(values: string[]): string | undefined | null {
  if (values.length === 0) return undefined
  if (values.length !== 1) return null
  const date = new Date(values[0])
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

export function parseHistorySearchParams(params: URLSearchParams): ParsedHistoryFilters {
  const rawFields = params.getAll('historyField')
  const fields = [...new Set(rawFields)] as KnownChangedField[]
  const from = parseInstant(params.getAll('historyFrom'))
  const before = parseInstant(params.getAll('historyBefore'))
  const directions = params.getAll('historySort')
  const direction = (directions[0] ?? 'descending') as HistorySortDirection
  const fallback: BookHistoryFilters = {
    changedFields: fields.filter((field) => knownChangedFields.includes(field)),
    sortDirection: 'descending',
    limit: 5,
  }
  const fieldsValid = fields.every((field) => knownChangedFields.includes(field))
  const directionValid = directions.length <= 1 && (direction === 'ascending' || direction === 'descending')
  const rangeValid =
    from !== null &&
    before !== null &&
    (!from || !before || new Date(from).getTime() < new Date(before).getTime())

  if (!fieldsValid || rawFields.length !== fields.length || !directionValid || !rangeValid) {
    return { valid: false, value: fallback }
  }
  return {
    valid: true,
    value: {
      changedFields: fields,
      changedFrom: from,
      changedBefore: before,
      sortDirection: direction,
      limit: 5,
    },
  }
}

export function writeHistorySearchParams(
  current: URLSearchParams,
  filters: BookHistoryFilters,
): URLSearchParams {
  const next = new URLSearchParams(current)
  for (const key of ['historyField', 'historyFrom', 'historyBefore', 'historySort']) next.delete(key)
  for (const field of filters.changedFields) next.append('historyField', field)
  if (filters.changedFrom) next.set('historyFrom', filters.changedFrom)
  if (filters.changedBefore) next.set('historyBefore', filters.changedBefore)
  if (filters.sortDirection !== 'descending') next.set('historySort', filters.sortDirection)
  return next
}

export function localDateTimeValue(instant?: string): string {
  if (!instant) return ''
  const date = new Date(instant)
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

export function localDateTimeToInstant(value: FormDataEntryValue | null): string | undefined {
  if (!value) return undefined
  const date = new Date(String(value))
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString()
}
