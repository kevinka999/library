import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { i18n } from '../../i18n'
import { renderApp } from '../../test/render-app'
import { server } from '../../test/server'
import { bookKeys } from '../../api/books/query-keys'
import { parseBooksSearchParams, serializeBooksUrlState } from './search-params'

const apiUrl = 'http://localhost:5168/api/books'

const firstPage = {
  items: [
    {
      id: 9,
      title: 'Zebra Book',
      shortDescription: 'Last alphabetically but first from API',
      publishDate: '2020-01-02',
      authors: ['Zoe Author'],
      version: 1,
    },
    {
      id: 2,
      title: 'Alpha Book',
      shortDescription: 'Second from API',
      publishDate: '2021-03-04',
      authors: ['Ada Author', 'Ben Author'],
      version: 2,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 22,
  totalPages: 2,
}

describe('books page', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it('parses and serializes default, custom, and invalid URL state', () => {
    expect(parseBooksSearchParams(new URLSearchParams())).toEqual({
      valid: true,
      value: { search: '', page: 1, pageSize: 20 },
    })
    expect(parseBooksSearchParams(new URLSearchParams('search=ada&page=3&pageSize=50'))).toEqual({
      valid: true,
      value: { search: 'ada', page: 3, pageSize: 50 },
    })
    expect(parseBooksSearchParams(new URLSearchParams('page=1&page=2')).valid).toBe(false)
    expect(serializeBooksUrlState({ search: 'ada', page: 3, pageSize: 50 }).toString()).toBe(
      'search=ada&page=3&pageSize=50',
    )
  })

  it('preserves API order and renders stable detail links', async () => {
    server.use(http.get(apiUrl, () => HttpResponse.json(firstPage)))
    renderApp()
    const rows = await screen.findAllByRole('row')
    expect(within(rows[1]).getByText('Zebra Book')).toBeVisible()
    expect(within(rows[2]).getByText('Alpha Book')).toBeVisible()
    expect(within(rows[1]).getByRole('link', { name: 'View details' })).toHaveAttribute('href', '/books/9')
  })

  it('resets the page for searches and preserves state for pagination', async () => {
    server.use(
      http.get(apiUrl, ({ request }) => {
        const url = new URL(request.url)
        const page = Number(url.searchParams.get('page'))
        return HttpResponse.json({ ...firstPage, page })
      }),
    )
    const user = userEvent.setup()
    const { router } = renderApp(['/books?search=author&page=2&pageSize=20'])
    await screen.findByText('Zebra Book')
    const search = screen.getByRole('textbox', { name: 'Search books' })
    await user.clear(search)
    await user.type(search, 'Ada')
    await user.click(screen.getByRole('button', { name: 'Search' }))
    expect(router.state.location.search).toBe('?search=Ada')

    await screen.findByText('Zebra Book')
    await user.click(screen.getByRole('button', { name: 'Next' }))
    expect(router.state.location.search).toBe('?search=Ada&page=2')
  })

  it('shows invalid URL feedback without requesting books', async () => {
    const request = vi.fn()
    server.use(http.get(apiUrl, () => {
      request()
      return HttpResponse.json(firstPage)
    }))
    renderApp(['/books?page=0'])
    expect(await screen.findByRole('heading', { name: 'Invalid book filters' })).toBeVisible()
    expect(request).not.toHaveBeenCalled()
  })

  it('validates a draft and creates a book with its ETag cached', async () => {
    server.use(
      http.get(apiUrl, () => HttpResponse.json(firstPage)),
      http.post(apiUrl, async ({ request }) => {
        const input = await request.json() as Record<string, unknown>
        expect(input).toEqual({
          title: 'New Book',
          shortDescription: 'A useful description',
          publishDate: '2024-05-06',
          authors: ['First Author', 'Second Author'],
        })
        return HttpResponse.json(
          { id: 42, ...input, version: 1 },
          { status: 201, headers: { ETag: '"version-1"' } },
        )
      }),
    )
    const user = userEvent.setup()
    const { router, queryClient } = renderApp()
    await screen.findByText('Zebra Book')
    await user.click(screen.getByRole('button', { name: 'Add book' }))
    await user.click(screen.getByRole('button', { name: 'Create book' }))
    expect(await screen.findAllByText('This field is required.')).toHaveLength(3)

    await user.type(screen.getByLabelText('Title'), 'New Book')
    await user.type(screen.getByLabelText('Short description'), 'A useful description')
    await user.type(screen.getByLabelText('Publish date'), '2024-05-06')
    await user.type(screen.getByLabelText('Author 1'), 'First Author')
    await user.click(screen.getByRole('button', { name: 'Add author' }))
    await user.type(screen.getByLabelText('Author 2'), 'Second Author')
    await user.click(screen.getByRole('button', { name: 'Create book' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/books/42'))
    expect(queryClient.getQueryData(bookKeys.detail(42))).toMatchObject({
      etag: '"version-1"',
      book: { id: 42, title: 'New Book' },
    })
  })

  it('keeps the creation draft when the API rejects a field', async () => {
    server.use(
      http.get(apiUrl, () => HttpResponse.json(firstPage)),
      http.post(apiUrl, () =>
        HttpResponse.json(
          { status: 400, errors: { Title: ['Server-authored message'] } },
          { status: 400 },
        ),
      ),
    )
    const user = userEvent.setup()
    renderApp()
    await screen.findByText('Zebra Book')
    await user.click(screen.getByRole('button', { name: 'Add book' }))
    await user.type(screen.getByLabelText('Title'), 'Preserved title')
    await user.type(screen.getByLabelText('Short description'), 'Description')
    await user.type(screen.getByLabelText('Publish date'), '2024-05-06')
    await user.type(screen.getByLabelText('Author 1'), 'Author')
    await user.click(screen.getByRole('button', { name: 'Create book' }))

    expect(await screen.findByText('The API rejected this value.')).toBeVisible()
    expect(screen.getByLabelText('Title')).toHaveValue('Preserved title')
    expect(screen.queryByText('Server-authored message')).not.toBeInTheDocument()
  })

  it('reports a successful response without an ETag as an integration failure', async () => {
    server.use(
      http.get(apiUrl, () => HttpResponse.json(firstPage)),
      http.post(apiUrl, () =>
        HttpResponse.json(
          {
            id: 42,
            title: 'New Book',
            shortDescription: 'Description',
            publishDate: '2024-05-06',
            authors: ['Author'],
            version: 1,
          },
          { status: 201 },
        ),
      ),
    )
    const user = userEvent.setup()
    renderApp()
    await screen.findByText('Zebra Book')
    await user.click(screen.getByRole('button', { name: 'Add book' }))
    await user.type(screen.getByLabelText('Title'), 'New Book')
    await user.type(screen.getByLabelText('Short description'), 'Description')
    await user.type(screen.getByLabelText('Publish date'), '2024-05-06')
    await user.type(screen.getByLabelText('Author 1'), 'Author')
    await user.click(screen.getByRole('button', { name: 'Create book' }))
    expect(await screen.findByText('The API returned an incomplete response.')).toBeVisible()
  })
})
