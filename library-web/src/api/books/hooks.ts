import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createBook } from './create-book'
import type { CreateBookInput } from './create-book'
import { getBooks, type GetBooksInput } from './get-books'
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
      void queryClient.invalidateQueries({ queryKey: bookKeys.lists() })
    },
  })
}
