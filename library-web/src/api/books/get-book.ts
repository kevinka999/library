import type { Book, BookWithEtag } from '../../types/book'
import { apiClient, normalizeApiError, requireEtag } from '../client'

export async function getBook(id: number): Promise<BookWithEtag> {
  try {
    const response = await apiClient.get<Book>(`/api/books/${id}`)
    return { book: response.data, etag: requireEtag(response.headers.etag) }
  } catch (error) {
    throw normalizeApiError(error)
  }
}
