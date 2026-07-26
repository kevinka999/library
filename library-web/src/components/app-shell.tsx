import { Link, Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { BookOpen } from 'lucide-react'
import { LanguageSelect } from './language-select'

export function AppShell() {
  const { t } = useTranslation()
  return (
    <div className="min-h-screen">
      <header className="border-b border-border bg-surface/95">
        <div className="mx-auto flex min-h-[var(--header-height)] max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
          <Link to="/books" className="flex items-center gap-3 rounded-md text-ink no-underline">
            <span className="grid size-10 place-items-center rounded-xl bg-primary text-white" aria-hidden="true">
              <BookOpen size={22} />
            </span>
            <span>
              <span className="block font-serif text-xl font-semibold leading-none">{t('common.productName')}</span>
              <span className="mt-1 hidden text-xs text-muted sm:block">{t('common.productTagline')}</span>
            </span>
          </Link>
          <LanguageSelect />
        </div>
      </header>
      <main id="main-content" className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12 lg:px-8">
        <Outlet />
      </main>
    </div>
  )
}
