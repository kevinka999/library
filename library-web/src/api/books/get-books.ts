import type { Book } from '../../types/book'
import { apiClient, normalizeApiError } from '../client'

export interface GetBooksInput {
  search: string
  page: number
  pageSize: number
}

export interface GetBooksOutput {
  items: Book[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export async function getBooks(input: GetBooksInput): Promise<GetBooksOutput> {
  try {
    const response = await apiClient.get<GetBooksOutput>('/api/books', {
      params: {
        search: input.search || undefined,
        page: input.page,
        pageSize: input.pageSize,
      },
    })
    return response.data
  } catch (error) {
    throw normalizeApiError(error)
  }
}
