import { http, HttpResponse } from 'msw'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { i18n } from '../../i18n'
import { renderApp } from '../../test/render-app'
import { server } from '../../test/server'
import { bookKeys } from '../../api/books/query-keys'

const detailUrl = 'http://localhost:5168/api/books/7'
const originalBook = {
  id: 7,
  title: 'The Original',
  shortDescription: 'Original description',
  publishDate: '1999-04-03',
  authors: ['First Author', 'Second Author'],
  version: 3,
}

describe('book details and editing', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
    server.use(
      http.get(`${detailUrl}/history`, () =>
        HttpResponse.json({ items: [], nextCursor: null, hasMore: false }),
      ),
    )
  })

  it('renders the complete current Book from one ETag-paired response', async () => {
    server.use(http.get(detailUrl, () => HttpResponse.json(originalBook, { headers: { ETag: '"opaque-v3"' } })))
    const { queryClient } = renderApp(['/books/7'])
    expect(await screen.findByRole('heading', { name: 'The Original' })).toBeVisible()
    expect(screen.getByText('Original description')).toBeVisible()
    expect(screen.getByText('April 3, 1999')).toBeVisible()
    expect(screen.getByText('First Author')).toBeVisible()
    expect(queryClient.getQueryData(bookKeys.detail(7))).toMatchObject({ etag: '"opaque-v3"' })
  })

  it('sends a complete replacement with the exact opaque ETag and updates the cache', async () => {
    server.use(
      http.get(detailUrl, () => HttpResponse.json(originalBook, { headers: { ETag: 'W/"odd-but-opaque"' } })),
      http.put(detailUrl, async ({ request }) => {
        expect(request.headers.get('If-Match')).toBe('W/"odd-but-opaque"')
        expect(await request.json()).toEqual({
          title: 'The Original',
          shortDescription: 'Revised description',
          publishDate: '1999-04-03',
          authors: ['First Author', 'Second Author'],
        })
        return HttpResponse.json(
          { ...originalBook, shortDescription: 'Revised description', version: 4 },
          { headers: { ETag: '"opaque-v4"' } },
        )
      }),
    )
    const user = userEvent.setup()
    const { queryClient } = renderApp(['/books/7'])
    await screen.findByRole('heading', { name: 'The Original' })
    await user.click(screen.getByRole('button', { name: 'Edit book' }))
    const description = screen.getByRole('textbox', { name: 'Short description' })
    await user.clear(description)
    await user.type(description, 'Revised description')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Revised description')).toBeVisible()
    expect(queryClient.getQueryData(bookKeys.detail(7))).toMatchObject({
      etag: '"opaque-v4"',
      book: { version: 4 },
    })
  })

  it('preserves a stale draft and reloads only after explicit confirmation', async () => {
    let getCount = 0
    server.use(
      http.get(detailUrl, () => {
        getCount += 1
        const book = getCount === 1 ? originalBook : { ...originalBook, title: 'Newer server title', version: 4 }
        return HttpResponse.json(book, { headers: { ETag: getCount === 1 ? '"v3"' : '"v4"' } })
      }),
      http.put(detailUrl, () => HttpResponse.json({ status: 412 }, { status: 412 })),
    )
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true)
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByRole('heading', { name: 'The Original' })
    await user.click(screen.getByRole('button', { name: 'Edit book' }))
    const title = screen.getByRole('textbox', { name: 'Title' })
    await user.clear(title)
    await user.type(title, 'My losing draft')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('This book has changed since you opened it.')).toBeVisible()
    expect(title).toHaveValue('My losing draft')
    await user.click(screen.getByRole('button', { name: 'Reload current book' }))
    expect(title).toHaveValue('My losing draft')
    expect(getCount).toBe(1)
    await user.click(screen.getByRole('button', { name: 'Reload current book' }))
    expect(await screen.findByRole('heading', { name: 'Newer server title' })).toBeVisible()
    expect(confirm).toHaveBeenCalledTimes(2)
  })

  it('requires confirmation before canceling a dirty edit', async () => {
    server.use(http.get(detailUrl, () => HttpResponse.json(originalBook, { headers: { ETag: '"v3"' } })))
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true)
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByRole('heading', { name: 'The Original' })
    await user.click(screen.getByRole('button', { name: 'Edit book' }))
    await user.type(screen.getByRole('textbox', { name: 'Title' }), ' changed')
    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(screen.getByRole('textbox', { name: 'Title' })).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('textbox', { name: 'Title' })).not.toBeInTheDocument()
    expect(confirm).toHaveBeenCalledTimes(2)
  })

  it('distinguishes not-found and missing successful ETags', async () => {
    server.use(http.get(detailUrl, () => HttpResponse.json({ status: 404 }, { status: 404 })))
    const first = renderApp(['/books/7'])
    expect(await screen.findByRole('heading', { name: 'Book not found' })).toBeVisible()
    first.unmount()

    server.use(http.get(detailUrl, () => HttpResponse.json(originalBook)))
    renderApp(['/books/7'])
    expect(await screen.findByRole('heading', { name: 'Editing is unavailable' })).toBeVisible()
    expect(screen.queryByRole('button', { name: 'Edit book' })).not.toBeInTheDocument()
  })

  it('keeps the edit draft for 428 and missing update ETags', async () => {
    let putCount = 0
    server.use(
      http.get(detailUrl, () => HttpResponse.json(originalBook, { headers: { ETag: '"v3"' } })),
      http.put(detailUrl, () => {
        putCount += 1
        if (putCount === 1) return HttpResponse.json({ status: 428 }, { status: 428 })
        return HttpResponse.json({ ...originalBook, title: 'Draft title' })
      }),
    )
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByRole('heading', { name: 'The Original' })
    await user.click(screen.getByRole('button', { name: 'Edit book' }))
    const title = screen.getByRole('textbox', { name: 'Title' })
    await user.clear(title)
    await user.type(title, 'Draft title')
    await user.click(screen.getByRole('button', { name: 'Save changes' }))
    expect(await screen.findByText('The API could not verify this update safely.')).toBeVisible()
    expect(title).toHaveValue('Draft title')

    await user.click(screen.getByRole('button', { name: 'Save changes' }))
    expect(await screen.findByText('The API returned an incomplete response.')).toBeVisible()
    expect(title).toHaveValue('Draft title')
  })

  it('localizes controls without translating Book values', async () => {
    await i18n.changeLanguage('de')
    server.use(http.get(detailUrl, () => HttpResponse.json(originalBook, { headers: { ETag: '"v3"' } })))
    renderApp(['/books/7'])
    expect(await screen.findByRole('heading', { name: 'The Original' })).toBeVisible()
    expect(screen.getByRole('button', { name: 'Buch bearbeiten' })).toBeVisible()
    expect(screen.getByText('Original description')).toBeVisible()
  })
})
