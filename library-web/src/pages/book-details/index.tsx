import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { buttonVariants } from '../../components/ui/button-styles'
import { parseBookId } from '../../lib/book-id'

export function BookDetailsPage() {
  const { bookId } = useParams()
  const { t } = useTranslation()
  const id = parseBookId(bookId)
  if (id === null) {
    return (
      <section className="mx-auto max-w-2xl rounded-2xl border border-border bg-surface p-6 shadow-card sm:p-8">
        <h1 className="font-serif text-3xl font-semibold">{t('errors.invalidBookIdTitle')}</h1>
        <p className="mt-3 text-muted">{t('errors.invalidBookId')}</p>
        <Link className={`${buttonVariants({ variant: 'secondary' })} mt-6 no-underline`} to="/books">
          {t('common.backToBooks')}
        </Link>
      </section>
    )
  }
  return (
    <section className="rounded-2xl border border-border bg-surface p-6 shadow-card sm:p-8">
      <h1 className="font-serif text-3xl font-semibold">{t('books.heading')} #{id}</h1>
      <p className="mt-3 text-muted">{t('books.foundationPending')}</p>
    </section>
  )
}
