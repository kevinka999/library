import { http, HttpResponse } from 'msw'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { i18n } from '../../i18n'
import { renderApp } from '../../test/render-app'
import { server } from '../../test/server'
import { parseHistorySearchParams, writeHistorySearchParams } from './history-search-params'

const detailUrl = 'http://localhost:5168/api/books/7'
const historyUrl = `${detailUrl}/history`
const book = {
  id: 7,
  title: 'History Book',
  shortDescription: 'Current description',
  publishDate: '2024-01-02',
  authors: ['Current Author'],
  version: 5,
}

const multiFieldItem = {
  changeSetId: '11111111-1111-1111-1111-111111111111',
  changedAt: '2026-07-26T10:30:00Z',
  changes: [
    { id: 1, changedField: 'title', oldValue: 'Old title', newValue: 'New title' },
    {
      id: 2,
      changedField: 'authors',
      oldValue: ['Alice'],
      newValue: ['Alice', 'Bob'],
    },
    {
      id: 3,
      changedField: 'publishDate',
      oldValue: '2020-01-01',
      newValue: '2021-02-03',
    },
    {
      id: 4,
      changedField: 'futureField',
      oldValue: { nested: true },
      newValue: null,
    },
  ],
}

function useBookHandler() {
  server.use(http.get(detailUrl, () => HttpResponse.json(book, { headers: { ETag: '"v5"' } })))
}

describe('book history', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
    useBookHandler()
  })

  it('parses normalized filters, rejects inverted ranges, and preserves unrelated URL state', () => {
    const parsed = parseHistorySearchParams(
      new URLSearchParams(
        'historyField=title&historyField=authors&historyFrom=2026-01-01T00%3A00%3A00Z&historyBefore=2026-02-01T00%3A00%3A00Z&historySort=ascending',
      ),
    )
    expect(parsed).toMatchObject({
      valid: true,
      value: {
        changedFields: ['title', 'authors'],
        changedFrom: '2026-01-01T00:00:00.000Z',
        changedBefore: '2026-02-01T00:00:00.000Z',
        sortDirection: 'ascending',
      },
    })
    expect(
      parseHistorySearchParams(
        new URLSearchParams(
          'historyFrom=2026-02-01T00%3A00%3A00Z&historyBefore=2026-01-01T00%3A00%3A00Z',
        ),
      ).valid,
    ).toBe(false)

    const written = writeHistorySearchParams(new URLSearchParams('unrelated=kept'), {
      changedFields: ['title'],
      changedFrom: '2026-01-01T00:00:00.000Z',
      sortDirection: 'ascending',
      limit: 20,
    })
    expect(written.get('unrelated')).toBe('kept')
    expect(written.getAll('historyField')).toEqual(['title'])
    expect(written.get('historySort')).toBe('ascending')
  })

  it('renders one timeline item for a complete multi-field Change Set', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({ items: [multiFieldItem], nextCursor: null, hasMore: false }),
      ),
    )
    renderApp(['/books/7?historyField=title'])
    expect(await screen.findByRole('heading', { name: 'History Book' })).toBeVisible()
    const articles = await screen.findAllByRole('article')
    expect(articles).toHaveLength(1)
    expect(within(articles[0]).getByText('Changed Title from Old title to New title')).toBeVisible()
    expect(within(articles[0]).getByText(/Changed Authors from Alice to Alice and Bob/)).toBeVisible()
    expect(within(articles[0]).getByText(/Changed Publish date from January 1, 2020 to February 3, 2021/)).toBeVisible()
    expect(within(articles[0]).getByText(/Removed futureField: \{"nested":true\}/)).toBeVisible()
    expect(within(articles[0]).getAllByRole('listitem')).toHaveLength(4)
  })

  it('passes the exact opaque cursor and appends the next page in order', async () => {
    const cursors: Array<string | null> = []
    server.use(
      http.get(historyUrl, ({ request }) => {
        const after = new URL(request.url).searchParams.get('after')
        cursors.push(after)
        if (!after) {
          return HttpResponse.json({
            items: [multiFieldItem],
            nextCursor: 'opaque+/= cursor',
            hasMore: true,
          })
        }
        return HttpResponse.json({
          items: [{
            changeSetId: '22222222-2222-2222-2222-222222222222',
            changedAt: '2026-07-25T10:30:00Z',
            changes: [{ id: 5, changedField: 'title', oldValue: 'Earlier', newValue: 'Old title' }],
          }],
          nextCursor: null,
          hasMore: false,
        })
      }),
    )
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByText('Changed Title from Old title to New title')
    await user.click(screen.getByRole('button', { name: 'Load more' }))
    expect(await screen.findByText('Changed Title from Earlier to Old title')).toBeVisible()
    expect(screen.getAllByRole('article')).toHaveLength(2)
    expect(cursors).toEqual([null, 'opaque+/= cursor'])
    expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument()
  })

  it('starts a fresh cursor chain when filters change and keeps sibling changes', async () => {
    const requests: Array<{ field: string | null; after: string | null }> = []
    server.use(
      http.get(historyUrl, ({ request }) => {
        const params = new URL(request.url).searchParams
        requests.push({ field: params.get('changedField'), after: params.get('after') })
        return HttpResponse.json({
          items: [multiFieldItem],
          nextCursor: params.has('changedField') ? null : 'next',
          hasMore: !params.has('changedField'),
        })
      }),
    )
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByText('Changed Title from Old title to New title')
    await user.click(screen.getByRole('checkbox', { name: 'Title' }))
    await user.click(screen.getByRole('button', { name: 'Apply filters' }))

    await waitFor(() => expect(requests).toContainEqual({ field: 'title', after: null }))
    expect(screen.getByText(/Changed Authors from Alice to Alice and Bob/)).toBeVisible()
  })

  it('preserves rendered history when a next page fails and retries it', async () => {
    let nextAttempts = 0
    server.use(
      http.get(historyUrl, ({ request }) => {
        const after = new URL(request.url).searchParams.get('after')
        if (!after) {
          return HttpResponse.json({ items: [multiFieldItem], nextCursor: 'next', hasMore: true })
        }
        nextAttempts += 1
        if (nextAttempts === 1) return HttpResponse.json({ status: 500 }, { status: 500 })
        return HttpResponse.json({
          items: [{
            changeSetId: '33333333-3333-3333-3333-333333333333',
            changedAt: '2026-07-24T10:30:00Z',
            changes: [{ id: 6, changedField: 'title', oldValue: null, newValue: 'Created title' }],
          }],
          nextCursor: null,
          hasMore: false,
        })
      }),
    )
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByText('Changed Title from Old title to New title')
    await user.click(screen.getByRole('button', { name: 'Load more' }))
    expect(await screen.findByText(/next history page could not be loaded/i)).toBeVisible()
    expect(screen.getByText('Changed Title from Old title to New title')).toBeVisible()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByText('Added Title: Created title')).toBeVisible()
    expect(nextAttempts).toBe(2)
  })

  it('keeps current details usable when history fails', async () => {
    server.use(http.get(historyUrl, () => HttpResponse.json({ status: 500 }, { status: 500 })))
    renderApp(['/books/7'])
    expect(await screen.findByRole('heading', { name: 'History Book' })).toBeVisible()
    expect(await screen.findByRole('heading', { name: 'History is unavailable' })).toBeVisible()
    expect(screen.getByText('Current description')).toBeVisible()
    expect(screen.getByRole('button', { name: 'Edit book' })).toBeEnabled()
  })

  it('localizes descriptions while preserving history values', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({ items: [multiFieldItem], nextCursor: null, hasMore: false }),
      ),
    )
    const user = userEvent.setup()
    renderApp(['/books/7'])
    await screen.findByText('Changed Title from Old title to New title')
    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'de')
    expect(await screen.findByText('Titel von Old title zu New title geändert')).toBeVisible()
    expect(screen.getByText(/Autoren von Alice zu Alice und Bob geändert/)).toBeVisible()
  })

  it('does not request history for an invalid range', async () => {
    const request = vi.fn()
    server.use(http.get(historyUrl, () => {
      request()
      return HttpResponse.json({ items: [], nextCursor: null, hasMore: false })
    }))
    renderApp([
      '/books/7?historyFrom=2026-02-01T00%3A00%3A00Z&historyBefore=2026-01-01T00%3A00%3A00Z',
    ])
    expect(await screen.findByRole('heading', { name: 'History Book' })).toBeVisible()
    expect(await screen.findByRole('heading', { name: 'Invalid history filters' })).toBeVisible()
    expect(request).not.toHaveBeenCalled()
  })
})
