import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient } from './api/client'
import { i18n, LANGUAGE_STORAGE_KEY, resolveInitialLanguage } from './i18n'
import { renderApp } from './test/render-app'
import { server } from './test/server'
import { http, HttpResponse } from 'msw'

describe('application foundation', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it('redirects the root route to the books page', async () => {
    const { router } = renderApp(['/'])
    await screen.findByRole('heading', { name: 'Books' })
    expect(router.state.location.pathname).toBe('/books')
  })

  it('renders localized unknown and invalid-book routes as distinct states', async () => {
    const getSpy = vi.spyOn(apiClient, 'get')
    const first = renderApp(['/books/not-an-id'])
    expect(await screen.findByRole('heading', { name: 'Invalid book address' })).toBeVisible()
    expect(getSpy).not.toHaveBeenCalled()
    first.unmount()

    renderApp(['/missing'])
    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeVisible()
  })

  it('switches and persists language while updating html lang', async () => {
    const user = userEvent.setup()
    renderApp()
    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'de')
    expect(await screen.findByRole('heading', { name: 'Bücher' })).toBeVisible()
    expect(document.documentElement).toHaveAttribute('lang', 'de')
    expect(window.localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('de')
  })

  it('falls back to supported browser languages and then English', () => {
    expect(resolveInitialLanguage('unsupported', 'de-AT')).toBe('de')
    expect(resolveInitialLanguage(null, 'fr-FR')).toBe('en')
    expect(resolveInitialLanguage('en', 'de-AT')).toBe('en')
  })

  it('keeps valid book routes inside the application shell', async () => {
    server.use(
      http.get('http://localhost:5168/api/books/12', () =>
        HttpResponse.json(
          {
            id: 12,
            title: 'Route Book',
            shortDescription: 'Description',
            publishDate: '2020-01-01',
            authors: ['Author'],
            version: 1,
          },
          { headers: { ETag: '"v1"' } },
        ),
      ),
      http.get('http://localhost:5168/api/books/12/history', () =>
        HttpResponse.json({ items: [], nextCursor: null, hasMore: false }),
      ),
    )
    renderApp(['/books/12'])
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Route Book' })).toBeVisible()
    })
    expect(screen.getByRole('link', { name: /Library/ })).toBeVisible()
  })
})
