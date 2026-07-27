import type { GetBooksInput } from './get-books'
import type { BookHistoryFilters } from './get-book-history'

export const bookKeys = {
  all: ['books'] as const,
  lists: () => [...bookKeys.all, 'list'] as const,
  list: (input: GetBooksInput) =>
    [...bookKeys.lists(), input.search, input.page, input.pageSize] as const,
  details: () => [...bookKeys.all, 'detail'] as const,
  detail: (id: number) => [...bookKeys.details(), id] as const,
  histories: () => [...bookKeys.all, 'history'] as const,
  historiesForBook: (id: number) => [...bookKeys.histories(), id] as const,
  history: (id: number, filters: BookHistoryFilters) =>
    [
      ...bookKeys.historiesForBook(id),
      filters.changedFields,
      filters.changedFrom,
      filters.changedBefore,
      filters.sortDirection,
      filters.limit,
    ] as const,
}
