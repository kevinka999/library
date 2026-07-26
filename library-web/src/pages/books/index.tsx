import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Plus, Search } from 'lucide-react'
import { ApiError } from '../../api/client'
import { useBooksQuery } from '../../api/books/hooks'
import { Button } from '../../components/ui/button'
import { buttonVariants } from '../../components/ui/button-styles'
import { Input } from '../../components/ui/input'
import { formatDate, formatNumber } from '../../i18n'
import { cn } from '../../lib/utils'
import { CreateBookDialog } from './create-book-dialog'
import { parseBooksSearchParams, serializeBooksUrlState } from './search-params'

function pageNumbers(current: number, total: number) {
  if (total <= 7) return Array.from({ length: total }, (_, index) => index + 1)
  const values = new Set([1, total, current - 1, current, current + 1])
  return [...values].filter((page) => page > 0 && page <= total).sort((a, b) => a - b)
}

export function BooksPage() {
  const { t, i18n } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)
  const parsed = parseBooksSearchParams(searchParams)
  const booksQuery = useBooksQuery(parsed.value, parsed.valid)

  function setUrl(next: typeof parsed.value) {
    setSearchParams(serializeBooksUrlState(next))
  }

  function submitSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    setUrl({ ...parsed.value, search: String(data.get('search') ?? '').trim(), page: 1 })
  }

  return (
    <section>
      <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
        <div>
          <p className="mb-2 text-sm font-semibold uppercase tracking-[0.16em] text-primary">{t('common.productName')}</p>
          <h1 className="font-serif text-4xl font-semibold tracking-tight sm:text-5xl">{t('books.heading')}</h1>
          <p className="mt-3 max-w-2xl text-muted">{t('books.introduction')}</p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus size={18} aria-hidden="true" />
          {t('books.add')}
        </Button>
      </div>

      <div className="mt-8 rounded-2xl border border-border bg-surface shadow-card">
        <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-end sm:justify-between sm:p-6">
          <form className="flex w-full max-w-2xl flex-col gap-2 sm:flex-row sm:items-end" onSubmit={submitSearch}>
            <label className="grow text-sm font-semibold text-ink">
              {t('books.searchLabel')}
              <Input
                key={parsed.value.search}
                name="search"
                defaultValue={parsed.value.search}
                placeholder={t('books.searchPlaceholder')}
                className="mt-1.5"
              />
            </label>
            <Button type="submit" variant="secondary">
              <Search size={18} aria-hidden="true" />
              {t('books.search')}
            </Button>
          </form>
          <label className="text-sm font-semibold text-ink">
            {t('pagination.pageSize')}
            <select
              className="ml-2 min-h-11 rounded-lg border border-border bg-surface px-3"
              value={parsed.value.pageSize}
              onChange={(event) => setUrl({ ...parsed.value, page: 1, pageSize: Number(event.target.value) })}
            >
              {[10, 20, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}
            </select>
          </label>
        </div>

        {!parsed.valid ? (
          <div className="p-6" role="alert">
            <h2 className="font-serif text-2xl font-semibold">{t('errors.invalidQueryTitle')}</h2>
            <p className="mt-2 text-muted">{t('errors.invalidQuery')}</p>
            <Button className="mt-4" variant="secondary" onClick={() => setUrl({ search: '', page: 1, pageSize: 20 })}>
              {t('books.resetFilters')}
            </Button>
          </div>
        ) : booksQuery.isPending ? (
          <p className="p-8 text-center text-muted" role="status">{t('books.loading')}</p>
        ) : booksQuery.isError ? (
          <div className="p-6" role="alert">
            <h2 className="font-serif text-2xl font-semibold">
              {booksQuery.error instanceof ApiError && booksQuery.error.kind === 'validation'
                ? t('errors.apiValidationTitle')
                : t('errors.unexpectedTitle')}
            </h2>
            <p className="mt-2 text-muted">
              {booksQuery.error instanceof ApiError && booksQuery.error.kind === 'validation'
                ? t('errors.apiValidation')
                : t('errors.unexpected')}
            </p>
            <Button className="mt-4" variant="secondary" onClick={() => void booksQuery.refetch()}>{t('common.retry')}</Button>
          </div>
        ) : booksQuery.data.items.length === 0 ? (
          <div className="p-8 text-center">
            <h2 className="font-serif text-2xl font-semibold">{t('books.emptyTitle')}</h2>
            <p className="mt-2 text-muted">{t('books.empty')}</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[46rem] border-collapse text-left">
                <thead>
                  <tr className="border-b border-border bg-canvas/60 text-sm text-muted">
                    <th className="px-6 py-3 font-semibold">{t('forms.title')}</th>
                    <th className="px-6 py-3 font-semibold">{t('forms.authors')}</th>
                    <th className="px-6 py-3 font-semibold">{t('forms.publishDate')}</th>
                    <th className="px-6 py-3 text-right font-semibold">{t('common.actions')}</th>
                  </tr>
                </thead>
                <tbody>
                  {booksQuery.data.items.map((book) => (
                    <tr key={book.id} className="border-b border-border last:border-0 hover:bg-primary-soft/40">
                      <td className="px-6 py-4 font-semibold">{book.title}</td>
                      <td className="px-6 py-4 text-muted">{book.authors.join(', ')}</td>
                      <td className="whitespace-nowrap px-6 py-4 text-muted">{formatDate(book.publishDate, i18n.resolvedLanguage)}</td>
                      <td className="px-6 py-4 text-right">
                        <Link className={cn(buttonVariants({ variant: 'ghost', size: 'small' }), 'no-underline')} to={`/books/${book.id}`}>
                          {t('books.viewDetails')}
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <nav className="flex flex-col items-center justify-between gap-4 border-t border-border p-4 sm:flex-row sm:p-6" aria-label={t('pagination.label')}>
              <p className="text-sm text-muted">
                {t('pagination.summary', {
                  page: formatNumber(booksQuery.data.page, i18n.resolvedLanguage),
                  totalPages: formatNumber(booksQuery.data.totalPages, i18n.resolvedLanguage),
                  totalCount: formatNumber(booksQuery.data.totalCount, i18n.resolvedLanguage),
                })}
              </p>
              <div className="flex flex-wrap items-center justify-center gap-1">
                <Button
                  variant="secondary"
                  size="small"
                  disabled={booksQuery.data.page <= 1}
                  onClick={() => setUrl({ ...parsed.value, page: booksQuery.data.page - 1 })}
                >
                  {t('pagination.previous')}
                </Button>
                {pageNumbers(booksQuery.data.page, booksQuery.data.totalPages).map((page, index, pages) => (
                  <span key={page} className="contents">
                    {index > 0 && page - pages[index - 1] > 1 && <span className="px-1 text-muted" aria-hidden="true">…</span>}
                    <Button
                      variant={page === booksQuery.data.page ? 'primary' : 'ghost'}
                      size="small"
                      aria-label={t('pagination.goToPage', { page })}
                      aria-current={page === booksQuery.data.page ? 'page' : undefined}
                      onClick={() => setUrl({ ...parsed.value, page })}
                    >
                      {page}
                    </Button>
                  </span>
                ))}
                <Button
                  variant="secondary"
                  size="small"
                  disabled={booksQuery.data.page >= booksQuery.data.totalPages}
                  onClick={() => setUrl({ ...parsed.value, page: booksQuery.data.page + 1 })}
                >
                  {t('pagination.next')}
                </Button>
              </div>
            </nav>
          </>
        )}
      </div>
      <CreateBookDialog open={createOpen} onOpenChange={setCreateOpen} />
    </section>
  )
}
