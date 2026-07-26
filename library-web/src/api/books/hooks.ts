import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createBook } from './create-book'
import type { CreateBookInput } from './create-book'
import { getBooks, type GetBooksInput } from './get-books'
import { getBook } from './get-book'
import { updateBook, type UpdateBookInput } from './update-book'
import { getBookHistory, type BookHistoryFilters } from './get-book-history'
import { bookKeys } from './query-keys'

export function useBooksQuery(input: GetBooksInput, enabled = true) {
  return useQuery({
    queryKey: bookKeys.list(input),
    queryFn: () => getBooks(input),
    enabled,
  })
}

export function useCreateBook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateBookInput) => createBook(input),
    onSuccess: (result) => {
      queryClient.setQueryData(bookKeys.detail(result.book.id), result)
      void queryClient.invalidateQueries({
        queryKey: bookKeys.lists(),
        refetchType: 'none',
      })
    },
  })
}

export function useBookQuery(id: number, enabled = true) {
  return useQuery({
    queryKey: bookKeys.detail(id),
    queryFn: () => getBook(id),
    enabled,
  })
}

export function useUpdateBook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: UpdateBookInput) => updateBook(input),
    onSuccess: async (result) => {
      queryClient.setQueryData(bookKeys.detail(result.book.id), result)
      void queryClient.invalidateQueries({
        queryKey: bookKeys.detail(result.book.id),
        refetchType: 'none',
      })
      void queryClient.invalidateQueries({
        queryKey: bookKeys.lists(),
        refetchType: 'none',
      })
      await queryClient.invalidateQueries({
        queryKey: bookKeys.historiesForBook(result.book.id),
      })
    },
  })
}

export function useBookHistoryQuery(
  id: number,
  filters: BookHistoryFilters,
  enabled = true,
) {
  return useInfiniteQuery({
    queryKey: bookKeys.history(id, filters),
    queryFn: ({ pageParam }) =>
      getBookHistory({ bookId: id, ...filters, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) =>
      lastPage.hasMore && lastPage.nextCursor ? lastPage.nextCursor : undefined,
    enabled,
  })
}
