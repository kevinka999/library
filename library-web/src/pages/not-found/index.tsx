import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { buttonVariants } from '../../components/ui/button-styles'

export function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <section className="mx-auto max-w-2xl rounded-2xl border border-border bg-surface p-6 shadow-card sm:p-8">
      <p className="text-sm font-semibold text-primary">404</p>
      <h1 className="mt-2 font-serif text-3xl font-semibold">{t('errors.notFoundTitle')}</h1>
      <p className="mt-3 text-muted">{t('errors.notFound')}</p>
      <Link className={`${buttonVariants({ variant: 'secondary' })} mt-6 no-underline`} to="/books">
        {t('common.backToBooks')}
      </Link>
    </section>
  )
}
