import type { Book, BookWithEtag } from '../../types/book'
import { apiClient, normalizeApiError, requireEtag } from '../client'

export interface CreateBookInput {
  title: string
  shortDescription: string
  publishDate: string
  authors: string[]
}

export async function createBook(input: CreateBookInput): Promise<BookWithEtag> {
  try {
    const response = await apiClient.post<Book>('/api/books', input)
    return {
      book: response.data,
      etag: requireEtag(response.headers.etag),
    }
  } catch (error) {
    throw normalizeApiError(error)
  }
}
