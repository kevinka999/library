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
        cancel: 'Cancel',
        close: 'Close',
        actions: 'Actions',
      },
      books: {
        heading: 'Books',
        introduction: 'Browse and manage the books in your library.',
        foundationPending: 'The book catalogue is ready to be connected.',
        add: 'Add book',
        searchLabel: 'Search books',
        searchPlaceholder: 'Title, description, or author',
        search: 'Search',
        resetFilters: 'Reset filters',
        loading: 'Loading books…',
        emptyTitle: 'No books found',
        empty: 'Try a different search or add the first book.',
        viewDetails: 'View details',
      },
      forms: {
        title: 'Title',
        shortDescription: 'Short description',
        publishDate: 'Publish date',
        authors: 'Authors',
        authorNumber: 'Author {{number}}',
        authorPlaceholder: 'Author name',
        addAuthor: 'Add author',
        removeAuthor: 'Remove author {{number}}',
        createTitle: 'Add a book',
        createDescription: 'Enter the complete catalogue information for this book.',
        create: 'Create book',
        creating: 'Creating…',
        edit: 'Edit book',
        save: 'Save changes',
        saving: 'Saving…',
        discardConfirm: 'Discard your unsaved changes?',
        reloadCurrent: 'Reload current book',
        reloadConfirm: 'Reloading will discard this draft. Continue?',
      },
      details: {
        loading: 'Loading book…',
        currentInformation: 'Current information',
        notFoundTitle: 'Book not found',
        notFound: 'This book does not exist or is no longer available.',
        integrationTitle: 'Editing is unavailable',
        integration: 'The API did not provide the version information required for a safe edit.',
      },
      validation: {
        required: 'This field is required.',
        max: 'Use {{count}} characters or fewer.',
        authorRequired: 'Author name must not be blank.',
        authorMinimum: 'Add at least one author.',
        authorUnique: 'Author names must be unique.',
        serverField: 'The API rejected this value.',
      },
      pagination: {
        label: 'Book pages',
        pageSize: 'Books per page',
        summary: 'Page {{page}} of {{totalPages}} · {{totalCount}} books',
        previous: 'Previous',
        next: 'Next',
        goToPage: 'Go to page {{page}}',
      },
      errors: {
        invalidBookIdTitle: 'Invalid book address',
        invalidBookId: 'Book IDs must be positive whole numbers. Return to the book list and choose a valid book.',
        notFoundTitle: 'Page not found',
        notFound: 'The page you requested does not exist. You can return to the book list.',
        unexpectedTitle: 'Something went wrong',
        unexpected: 'The library could not complete this request.',
        integration: 'The API returned an incomplete response.',
        validation: 'Some values were rejected. Review the marked fields.',
        invalidQueryTitle: 'Invalid book filters',
        invalidQuery: 'The URL contains invalid or repeated paging values. Reset the filters to continue.',
        apiValidationTitle: 'The filters were rejected',
        apiValidation: 'Reset the filters or choose valid paging values.',
        stale: 'This book has changed since you opened it.',
        precondition: 'The API could not verify this update safely.',
        'not-found': 'This book no longer exists.',
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
        cancel: 'Abbrechen',
        close: 'Schließen',
        actions: 'Aktionen',
      },
      books: {
        heading: 'Bücher',
        introduction: 'Durchsuchen und verwalten Sie Ihre Bibliothek.',
        foundationPending: 'Der Bücherkatalog kann jetzt verbunden werden.',
        add: 'Buch hinzufügen',
        searchLabel: 'Bücher suchen',
        searchPlaceholder: 'Titel, Beschreibung oder Autor',
        search: 'Suchen',
        resetFilters: 'Filter zurücksetzen',
        loading: 'Bücher werden geladen…',
        emptyTitle: 'Keine Bücher gefunden',
        empty: 'Versuchen Sie eine andere Suche oder fügen Sie das erste Buch hinzu.',
        viewDetails: 'Details anzeigen',
      },
      forms: {
        title: 'Titel',
        shortDescription: 'Kurzbeschreibung',
        publishDate: 'Erscheinungsdatum',
        authors: 'Autoren',
        authorNumber: 'Autor {{number}}',
        authorPlaceholder: 'Name des Autors',
        addAuthor: 'Autor hinzufügen',
        removeAuthor: 'Autor {{number}} entfernen',
        createTitle: 'Buch hinzufügen',
        createDescription: 'Geben Sie die vollständigen Kataloginformationen für dieses Buch ein.',
        create: 'Buch erstellen',
        creating: 'Wird erstellt…',
        edit: 'Buch bearbeiten',
        save: 'Änderungen speichern',
        saving: 'Wird gespeichert…',
        discardConfirm: 'Möchten Sie Ihre ungespeicherten Änderungen verwerfen?',
        reloadCurrent: 'Aktuelles Buch neu laden',
        reloadConfirm: 'Beim Neuladen wird dieser Entwurf verworfen. Fortfahren?',
      },
      details: {
        loading: 'Buch wird geladen…',
        currentInformation: 'Aktuelle Informationen',
        notFoundTitle: 'Buch nicht gefunden',
        notFound: 'Dieses Buch existiert nicht oder ist nicht mehr verfügbar.',
        integrationTitle: 'Bearbeitung ist nicht verfügbar',
        integration: 'Die API hat die für eine sichere Bearbeitung erforderliche Versionsinformation nicht bereitgestellt.',
      },
      validation: {
        required: 'Dieses Feld ist erforderlich.',
        max: 'Verwenden Sie höchstens {{count}} Zeichen.',
        authorRequired: 'Der Name des Autors darf nicht leer sein.',
        authorMinimum: 'Fügen Sie mindestens einen Autor hinzu.',
        authorUnique: 'Autorennamen müssen eindeutig sein.',
        serverField: 'Die API hat diesen Wert abgelehnt.',
      },
      pagination: {
        label: 'Buchseiten',
        pageSize: 'Bücher pro Seite',
        summary: 'Seite {{page}} von {{totalPages}} · {{totalCount}} Bücher',
        previous: 'Zurück',
        next: 'Weiter',
        goToPage: 'Zu Seite {{page}}',
      },
      errors: {
        invalidBookIdTitle: 'Ungültige Buchadresse',
        invalidBookId: 'Buch-IDs müssen positive ganze Zahlen sein. Kehren Sie zur Buchliste zurück und wählen Sie ein gültiges Buch.',
        notFoundTitle: 'Seite nicht gefunden',
        notFound: 'Die angeforderte Seite existiert nicht. Sie können zur Buchliste zurückkehren.',
        unexpectedTitle: 'Etwas ist schiefgegangen',
        unexpected: 'Die Bibliothek konnte diese Anfrage nicht abschließen.',
        integration: 'Die API hat eine unvollständige Antwort zurückgegeben.',
        validation: 'Einige Werte wurden abgelehnt. Prüfen Sie die markierten Felder.',
        invalidQueryTitle: 'Ungültige Buchfilter',
        invalidQuery: 'Die URL enthält ungültige oder wiederholte Seitenwerte. Setzen Sie die Filter zurück.',
        apiValidationTitle: 'Die Filter wurden abgelehnt',
        apiValidation: 'Setzen Sie die Filter zurück oder wählen Sie gültige Seitenwerte.',
        stale: 'Dieses Buch wurde geändert, seit Sie es geöffnet haben.',
        precondition: 'Die API konnte diese Änderung nicht sicher prüfen.',
        'not-found': 'Dieses Buch existiert nicht mehr.',
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
