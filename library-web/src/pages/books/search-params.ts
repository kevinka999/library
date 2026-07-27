export interface BooksUrlState {
  search: string
  page: number
  pageSize: number
}

export type ParsedBooksUrlState =
  | { valid: true; value: BooksUrlState }
  | { valid: false; value: BooksUrlState }

function parseInteger(values: string[], fallback: number): number | null {
  if (values.length === 0) return fallback
  if (values.length !== 1 || !/^[1-9]\d*$/.test(values[0])) return null
  const value = Number(values[0])
  if (!Number.isSafeInteger(value)) return null
  return value
}

export function parseBooksSearchParams(params: URLSearchParams): ParsedBooksUrlState {
  const searches = params.getAll('search')
  const page = parseInteger(params.getAll('page'), 1)
  const pageSize = parseInteger(params.getAll('pageSize'), 10)
  const fallback = { search: searches[0] ?? '', page: 1, pageSize: 10 }
  if (
    searches.length > 1 ||
    page === null ||
    pageSize === null ||
    ![5, 10].includes(pageSize)
  ) {
    return { valid: false, value: fallback }
  }
  return { valid: true, value: { search: searches[0] ?? '', page, pageSize } }
}

export function serializeBooksUrlState(value: BooksUrlState): URLSearchParams {
  const params = new URLSearchParams()
  if (value.search) params.set('search', value.search)
  if (value.page !== 1) params.set('page', String(value.page))
  if (value.pageSize !== 10) params.set('pageSize', String(value.pageSize))
  return params
}
