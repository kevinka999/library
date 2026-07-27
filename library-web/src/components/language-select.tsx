import { useTranslation } from 'react-i18next'

export function LanguageSelect() {
  const { i18n, t } = useTranslation()
  return (
    <label className="flex items-center gap-2 text-sm font-medium text-muted">
      <span className="hidden sm:inline">{t('common.language')}</span>
      <select
        aria-label={t('common.language')}
        className="min-h-11 rounded-lg border border-border bg-surface px-3 text-ink shadow-sm"
        value={i18n.resolvedLanguage === 'de' ? 'de' : 'en'}
        onChange={(event) => void i18n.changeLanguage(event.target.value)}
      >
        <option value="de">🇩🇪 Deutsch</option>
        <option value="en">🇬🇧 English</option>
      </select>
    </label>
  )
}
