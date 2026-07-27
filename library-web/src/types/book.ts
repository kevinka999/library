export interface Book {
  id: number
  title: string
  shortDescription: string
  publishDate: string
  authors: string[]
  version: number
}

export interface BookWithEtag {
  book: Book
  etag: string
}
