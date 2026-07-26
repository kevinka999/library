import i18next from 'i18next'
import { initReactI18next } from 'react-i18next'

export const LANGUAGE_STORAGE_KEY = 'library-language'
export type SupportedLanguage = 'en' | 'de'

export const resources = {
  en: {
    translation: {
      common: {
        productName: 'Library',
        productTagline: 'Books, thoughtfully catalogued',
        language: 'Language',
        loading: 'Loading…',
        retry: 'Try again',
        backToBooks: 'Back to books',
      },
      books: {
        heading: 'Books',
        introduction: 'Browse and manage the books in your library.',
        foundationPending: 'The book catalogue is ready to be connected.',
      },
      errors: {
        invalidBookIdTitle: 'Invalid book address',
        invalidBookId: 'Book IDs must be positive whole numbers. Return to the book list and choose a valid book.',
        notFoundTitle: 'Page not found',
        notFound: 'The page you requested does not exist. You can return to the book list.',
        unexpectedTitle: 'Something went wrong',
        unexpected: 'The library could not complete this request.',
        integration: 'The API returned an incomplete response.',
      },
    },
  },
  de: {
    translation: {
      common: {
        productName: 'Bibliothek',
        productTagline: 'Bücher, sorgfältig katalogisiert',
        language: 'Sprache',
        loading: 'Wird geladen…',
        retry: 'Erneut versuchen',
        backToBooks: 'Zurück zu den Büchern',
      },
      books: {
        heading: 'Bücher',
        introduction: 'Durchsuchen und verwalten Sie Ihre Bibliothek.',
        foundationPending: 'Der Bücherkatalog kann jetzt verbunden werden.',
      },
      errors: {
        invalidBookIdTitle: 'Ungültige Buchadresse',
        invalidBookId: 'Buch-IDs müssen positive ganze Zahlen sein. Kehren Sie zur Buchliste zurück und wählen Sie ein gültiges Buch.',
        notFoundTitle: 'Seite nicht gefunden',
        notFound: 'Die angeforderte Seite existiert nicht. Sie können zur Buchliste zurückkehren.',
        unexpectedTitle: 'Etwas ist schiefgegangen',
        unexpected: 'Die Bibliothek konnte diese Anfrage nicht abschließen.',
        integration: 'Die API hat eine unvollständige Antwort zurückgegeben.',
      },
    },
  },
} as const

export function resolveInitialLanguage(
  savedLanguage = window.localStorage.getItem(LANGUAGE_STORAGE_KEY),
  browserLanguage = window.navigator.language,
): SupportedLanguage {
  if (savedLanguage === 'en' || savedLanguage === 'de') return savedLanguage
  return browserLanguage.toLowerCase().startsWith('de') ? 'de' : 'en'
}

export const i18n = i18next.createInstance()

void i18n.use(initReactI18next).init({
  resources,
  lng: resolveInitialLanguage(),
  fallbackLng: 'en',
  supportedLngs: ['en', 'de'],
  interpolation: { escapeValue: false },
})

i18n.on('languageChanged', (language) => {
  const supported = language === 'de' ? 'de' : 'en'
  window.localStorage.setItem(LANGUAGE_STORAGE_KEY, supported)
  document.documentElement.lang = supported
})

document.documentElement.lang = resolveInitialLanguage()

export function formatDate(value: string, language = i18n.resolvedLanguage) {
  const [year, month, day] = value.split('-').map(Number)
  return new Intl.DateTimeFormat(language, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date(Date.UTC(year, month - 1, day)))
}

export function formatTimestamp(value: string, language = i18n.resolvedLanguage) {
  return new Intl.DateTimeFormat(language, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function formatNumber(value: number, language = i18n.resolvedLanguage) {
  return new Intl.NumberFormat(language).format(value)
}
