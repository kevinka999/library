import type { GetBooksInput } from './get-books'

export const bookKeys = {
  all: ['books'] as const,
  lists: () => [...bookKeys.all, 'list'] as const,
  list: (input: GetBooksInput) =>
    [...bookKeys.lists(), input.search, input.page, input.pageSize] as const,
  details: () => [...bookKeys.all, 'detail'] as const,
  detail: (id: number) => [...bookKeys.details(), id] as const,
  histories: () => [...bookKeys.all, 'history'] as const,
}
