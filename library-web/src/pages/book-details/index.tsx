import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, Pencil } from 'lucide-react'
import { ApiError } from '../../api/client'
import { useBookQuery } from '../../api/books/hooks'
import { Button } from '../../components/ui/button'
import { buttonVariants } from '../../components/ui/button-styles'
import { formatDate } from '../../i18n'
import { parseBookId } from '../../lib/book-id'
import { HistorySection } from './components/history-section'
import { EditBookDialog } from './components/edit-book-dialog'

function StateCard({ title, message, action }: { title: string; message: string; action?: React.ReactNode }) {
  return (
    <section className="mx-auto max-w-2xl rounded-2xl border border-border bg-surface p-6 shadow-card sm:p-8">
      <h1 className="font-serif text-3xl font-semibold">{title}</h1>
      <p className="mt-3 text-muted">{message}</p>
      {action}
    </section>
  )
}

export function BookDetailsPage() {
  const [editOpen, setEditOpen] = useState(false)
  const [disappeared, setDisappeared] = useState(false)
  const { bookId } = useParams()
  const { t, i18n } = useTranslation()
  const id = parseBookId(bookId)
  const bookQuery = useBookQuery(id ?? 0, id !== null)

  async function reloadCurrent() {
    const result = await bookQuery.refetch()
    return result.isSuccess
  }

  const backLink = (
    <Link className={`${buttonVariants({ variant: 'secondary' })} mt-6 no-underline`} to="/books">
      <ArrowLeft size={18} aria-hidden="true" />
      {t('common.backToBooks')}
    </Link>
  )

  if (id === null) {
    return <StateCard title={t('errors.invalidBookIdTitle')} message={t('errors.invalidBookId')} action={backLink} />
  }
  if (bookQuery.isPending) {
    return <p className="py-16 text-center text-muted" role="status">{t('details.loading')}</p>
  }
  if (disappeared || (bookQuery.error instanceof ApiError && bookQuery.error.kind === 'not-found')) {
    return <StateCard title={t('details.notFoundTitle')} message={t('details.notFound')} action={backLink} />
  }
  if (bookQuery.isError) {
    const integration = bookQuery.error instanceof ApiError && bookQuery.error.kind === 'integration'
    return (
      <StateCard
        title={integration ? t('details.integrationTitle') : t('errors.unexpectedTitle')}
        message={integration ? t('details.integration') : t('errors.unexpected')}
        action={
          <div className="mt-6 flex flex-wrap gap-3">
            <Button variant="secondary" onClick={() => void bookQuery.refetch()}>{t('common.retry')}</Button>
            {backLink}
          </div>
        }
      />
    )
  }

  const { book } = bookQuery.data

  return (
    <div>
      <Link className="inline-flex items-center gap-2 rounded-md text-sm font-semibold text-primary no-underline hover:underline" to="/books">
        <ArrowLeft size={17} aria-hidden="true" />
        {t('common.backToBooks')}
      </Link>
      <section className="mt-5 rounded-2xl border border-border bg-surface p-5 shadow-card sm:p-8" aria-labelledby="current-book-heading">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.16em] text-primary">{t('details.currentInformation')}</p>
            <h1 id="current-book-heading" className="mt-2 font-serif text-4xl font-semibold tracking-tight sm:text-5xl">
              {book.title}
            </h1>
          </div>
          <Button variant="secondary" onClick={() => setEditOpen(true)}>
            <Pencil size={17} aria-hidden="true" />
            {t('forms.edit')}
          </Button>
        </div>

        <dl className="mt-8 grid gap-6 border-t border-border pt-6 sm:grid-cols-2">
          <div>
            <dt className="text-sm font-semibold text-muted">{t('forms.shortDescription')}</dt>
            <dd className="mt-1 whitespace-pre-wrap text-ink">{book.shortDescription}</dd>
          </div>
          <div>
            <dt className="text-sm font-semibold text-muted">{t('forms.publishDate')}</dt>
            <dd className="mt-1 text-ink">{formatDate(book.publishDate, i18n.resolvedLanguage)}</dd>
          </div>
          <div>
            <dt className="text-sm font-semibold text-muted">{t('forms.authors')}</dt>
            <dd className="mt-1 text-ink">
              <ul className="space-y-1">
                {book.authors.map((author) => <li key={author}>{author}</li>)}
              </ul>
            </dd>
          </div>
        </dl>
      </section>
      <EditBookDialog
        open={editOpen}
        onOpenChange={setEditOpen}
        initialBook={bookQuery.data}
        onReloadCurrent={reloadCurrent}
        onNotFound={() => setDisappeared(true)}
      />
      <HistorySection bookId={book.id} />
    </div>
  )
}
