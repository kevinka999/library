import { useTranslation } from 'react-i18next'

export function BooksPage() {
  const { t } = useTranslation()
  return (
    <section className="rounded-2xl border border-border bg-surface p-6 shadow-card sm:p-8">
      <p className="mb-2 text-sm font-semibold uppercase tracking-[0.16em] text-primary">{t('common.productName')}</p>
      <h1 className="font-serif text-4xl font-semibold tracking-tight sm:text-5xl">{t('books.heading')}</h1>
      <p className="mt-3 max-w-2xl text-muted">{t('books.introduction')}</p>
      <p className="mt-8 rounded-xl bg-primary-soft p-4 text-primary">{t('books.foundationPending')}</p>
    </section>
  )
}
