import type { Book, BookWithEtag } from '../../types/book'
import { apiClient, normalizeApiError, requireEtag } from '../client'

export interface UpdateBookInput {
  id: number
  etag: string
  title: string
  shortDescription: string
  publishDate: string
  authors: string[]
}

export async function updateBook(input: UpdateBookInput): Promise<BookWithEtag> {
  try {
    const response = await apiClient.put<Book>(
      `/api/books/${input.id}`,
      {
        title: input.title,
        shortDescription: input.shortDescription,
        publishDate: input.publishDate,
        authors: input.authors,
      },
      { headers: { 'If-Match': input.etag } },
    )
    return { book: response.data, etag: requireEtag(response.headers.etag) }
  } catch (error) {
    throw normalizeApiError(error)
  }
}
