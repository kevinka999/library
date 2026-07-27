import { apiClient, normalizeApiError } from '../client'

export type HistorySortDirection = 'ascending' | 'descending'
export type KnownChangedField = 'title' | 'shortDescription' | 'publishDate' | 'authors'

export interface BookHistoryChange {
  id: number
  changedField: string
  oldValue: unknown
  newValue: unknown
}

export interface BookHistoryItem {
  changeSetId: string
  changedAt: string
  changes: BookHistoryChange[]
}

export interface BookHistoryFilters {
  changedFields: KnownChangedField[]
  changedFrom?: string
  changedBefore?: string
  sortDirection: HistorySortDirection
  limit: number
}

export interface GetBookHistoryInput extends BookHistoryFilters {
  bookId: number
  after?: string
}

export interface GetBookHistoryOutput {
  items: BookHistoryItem[]
  nextCursor: string | null
  hasMore: boolean
}

export async function getBookHistory(input: GetBookHistoryInput): Promise<GetBookHistoryOutput> {
  const params = new URLSearchParams()
  for (const field of input.changedFields) params.append('changedField', field)
  if (input.changedFrom) params.set('changedFrom', input.changedFrom)
  if (input.changedBefore) params.set('changedBefore', input.changedBefore)
  params.set('sortDirection', input.sortDirection)
  params.set('limit', String(input.limit))
  if (input.after) params.set('after', input.after)

  try {
    const response = await apiClient.get<GetBookHistoryOutput>(
      `/api/books/${input.bookId}/history`,
      { params },
    )
    return response.data
  } catch (error) {
    throw normalizeApiError(error)
  }
}
