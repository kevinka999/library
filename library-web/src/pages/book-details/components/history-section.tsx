import { useSearchParams } from 'react-router-dom'
import { Trans, useTranslation } from 'react-i18next'
import { Filter, RotateCcw } from 'lucide-react'
import type { PropsWithChildren } from 'react'
import type { BookHistoryChange, BookHistoryItem, KnownChangedField } from '../../../api/books/get-book-history'
import { useBookHistoryQuery } from '../../../api/books/hooks'
import { Button } from '../../../components/ui/button'
import { Input } from '../../../components/ui/input'
import { formatDate, formatTimestamp } from '../../../i18n'
import {
  knownChangedFields,
  localDateTimeToInstant,
  localDateTimeValue,
  parseHistorySearchParams,
  writeHistorySearchParams,
} from '../history-search-params'

function rawValue(value: unknown): string {
  if (typeof value === 'string') return value
  try {
    return JSON.stringify(value) ?? String(value)
  } catch {
    return String(value)
  }
}

function displayValue(
  value: unknown,
  field: string,
  language: string | undefined,
  none: string,
): string {
  if (value === null || value === undefined) return none
  if (Array.isArray(value)) {
    const values = value.map(rawValue)
    return values.length ? new Intl.ListFormat(language, { style: 'long', type: 'conjunction' }).format(values) : none
  }
  if (field === 'publishDate' && typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return formatDate(value, language)
  }
  return rawValue(value)
}

function HistoryValue({
  tone,
  children,
}: PropsWithChildren<{ tone: 'old' | 'new' }>) {
  return (
    <span
      data-history-value={tone}
      className={
        tone === 'old'
          ? 'box-decoration-clone rounded-md border border-danger/25 bg-danger-soft px-1.5 py-0.5 font-mono text-[0.85rem] font-medium text-danger line-through decoration-danger/50'
          : 'box-decoration-clone rounded-md border border-primary/20 bg-primary-soft px-1.5 py-0.5 font-mono text-[0.85rem] font-medium text-primary'
      }
    >
      {children}
    </span>
  )
}

function ChangeDescription({ change }: { change: BookHistoryChange }) {
  const { t, i18n } = useTranslation()
  const known = knownChangedFields.includes(change.changedField as KnownChangedField)
  const field = known ? t(`history.fields.${change.changedField}`) : change.changedField
  const oldValue = displayValue(change.oldValue, change.changedField, i18n.resolvedLanguage, t('history.none'))
  const newValue = displayValue(change.newValue, change.changedField, i18n.resolvedLanguage, t('history.none'))

  if (change.oldValue === null || change.oldValue === undefined) {
    return (
      <Trans
        i18nKey="history.added"
        values={{ field, value: newValue }}
        components={[
          <strong key="field" className="font-semibold text-ink" />,
          <HistoryValue key="new" tone="new" />,
        ]}
      />
    )
  }
  if (change.newValue === null || change.newValue === undefined) {
    return (
      <Trans
        i18nKey="history.removed"
        values={{ field, value: oldValue }}
        components={[
          <strong key="field" className="font-semibold text-ink" />,
          <HistoryValue key="old" tone="old" />,
        ]}
      />
    )
  }
  return (
    <Trans
      i18nKey="history.changed"
      values={{ field, oldValue, newValue }}
      components={[
        <strong key="field" className="font-semibold text-ink" />,
        <HistoryValue key="old" tone="old" />,
        <HistoryValue key="new" tone="new" />,
      ]}
    />
  )
}

function TimelineItem({ item, final }: { item: BookHistoryItem; final: boolean }) {
  const { t, i18n } = useTranslation()
  const created = item.changes.length > 0 && item.changes.every((change) => change.oldValue == null)
  return (
    <li className="relative grid grid-cols-[1.25rem_1fr] gap-4 pb-8 last:pb-0">
      <div className="relative flex justify-center" aria-hidden="true">
        <span className="mt-1.5 size-3 rounded-full border-[3px] border-surface bg-primary ring-2 ring-primary" />
        {!final && <span className="absolute bottom-[-0.5rem] top-5 w-px bg-border" />}
      </div>
      <article>
        <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:justify-between">
          <h3 className="font-serif text-xl font-semibold">{created ? t('history.created') : t('history.updated')}</h3>
          <time className="text-sm text-muted" dateTime={item.changedAt}>
            {formatTimestamp(item.changedAt, i18n.resolvedLanguage)}
          </time>
        </div>
        <ul className="mt-3 space-y-2 rounded-xl bg-canvas/70 p-4 text-sm">
          {item.changes.map((change) => (
            <li key={change.id} className="leading-relaxed">
              <ChangeDescription change={change} />
            </li>
          ))}
        </ul>
      </article>
    </li>
  )
}

export function HistorySection({ bookId }: { bookId: number }) {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const parsed = parseHistorySearchParams(searchParams)
  const historyQuery = useBookHistoryQuery(bookId, parsed.value, parsed.valid)
  const items = historyQuery.data?.pages.flatMap((page) => page.items) ?? []

  function applyFilters(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const fields = data.getAll('historyField') as KnownChangedField[]
    const next = {
      changedFields: fields,
      changedFrom: localDateTimeToInstant(data.get('historyFrom')),
      changedBefore: localDateTimeToInstant(data.get('historyBefore')),
      sortDirection: data.get('historySort') === 'ascending' ? 'ascending' as const : 'descending' as const,
      limit: 20,
    }
    setSearchParams(writeHistorySearchParams(searchParams, next))
  }

  function resetFilters() {
    setSearchParams(writeHistorySearchParams(searchParams, {
      changedFields: [],
      sortDirection: 'descending',
      limit: 20,
    }))
  }

  return (
    <section className="mt-8 rounded-2xl border border-border bg-surface p-5 shadow-card sm:p-8" aria-labelledby="history-heading">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.16em] text-primary">{t('history.eyebrow')}</p>
        <h2 id="history-heading" className="mt-2 font-serif text-3xl font-semibold sm:text-4xl">{t('history.heading')}</h2>
        <p className="mt-2 text-muted">{t('history.introduction')}</p>
      </div>

      <form
        key={`${parsed.value.changedFields.join(',')}:${parsed.value.changedFrom}:${parsed.value.changedBefore}:${parsed.value.sortDirection}`}
        className="mt-6 rounded-xl border border-border bg-canvas/55 p-4"
        onSubmit={applyFilters}
      >
        <fieldset>
          <legend className="text-sm font-semibold">{t('history.changedFields')}</legend>
          <div className="mt-3 flex flex-wrap gap-x-5 gap-y-3">
            {knownChangedFields.map((field) => (
              <label key={field} className="flex min-h-8 items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  name="historyField"
                  value={field}
                  defaultChecked={parsed.value.changedFields.includes(field)}
                  className="size-4 accent-primary"
                />
                {t(`history.fields.${field}`)}
              </label>
            ))}
          </div>
        </fieldset>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <label className="text-sm font-semibold">
            {t('history.changedFrom')}
            <Input name="historyFrom" type="datetime-local" className="mt-1.5" defaultValue={localDateTimeValue(parsed.value.changedFrom)} />
          </label>
          <label className="text-sm font-semibold">
            {t('history.changedBefore')}
            <Input name="historyBefore" type="datetime-local" className="mt-1.5" defaultValue={localDateTimeValue(parsed.value.changedBefore)} />
          </label>
          <label className="text-sm font-semibold">
            {t('history.sortDirection')}
            <select name="historySort" defaultValue={parsed.value.sortDirection} className="mt-1.5 min-h-11 w-full rounded-lg border border-border bg-surface px-3">
              <option value="descending">{t('history.newestFirst')}</option>
              <option value="ascending">{t('history.oldestFirst')}</option>
            </select>
          </label>
        </div>
        <div className="mt-4 flex flex-wrap gap-3">
          <Button type="submit" size="small">
            <Filter size={16} aria-hidden="true" />
            {t('history.applyFilters')}
          </Button>
          <Button type="button" variant="ghost" size="small" onClick={resetFilters}>
            <RotateCcw size={16} aria-hidden="true" />
            {t('history.resetFilters')}
          </Button>
        </div>
      </form>

      {!parsed.valid ? (
        <div className="mt-6 rounded-xl bg-danger-soft p-4 text-danger" role="alert">
          <h3 className="font-semibold">{t('history.invalidFiltersTitle')}</h3>
          <p className="mt-1 text-sm">{t('history.invalidFilters')}</p>
          <Button className="mt-3" variant="secondary" size="small" onClick={resetFilters}>{t('history.resetFilters')}</Button>
        </div>
      ) : historyQuery.isPending ? (
        <p className="py-12 text-center text-muted" role="status">{t('history.loading')}</p>
      ) : historyQuery.isError && items.length === 0 ? (
        <div className="mt-6 rounded-xl bg-danger-soft p-4 text-danger" role="alert">
          <h3 className="font-semibold">{t('history.errorTitle')}</h3>
          <p className="mt-1 text-sm">{t('history.error')}</p>
          <Button className="mt-3" variant="secondary" size="small" onClick={() => void historyQuery.refetch()}>{t('common.retry')}</Button>
        </div>
      ) : items.length === 0 ? (
        <div className="py-12 text-center">
          <h3 className="font-serif text-2xl font-semibold">{t('history.emptyTitle')}</h3>
          <p className="mt-2 text-muted">{t('history.empty')}</p>
        </div>
      ) : (
        <div className="mt-8">
          <ol>
            {items.map((item, index) => (
              <TimelineItem key={item.changeSetId} item={item} final={index === items.length - 1} />
            ))}
          </ol>
          {historyQuery.isFetchNextPageError && (
            <div className="mb-4 rounded-xl bg-danger-soft p-4 text-danger" role="alert">
              <p>{t('history.nextPageError')}</p>
              <Button className="mt-3" variant="secondary" size="small" onClick={() => void historyQuery.fetchNextPage()}>
                {t('common.retry')}
              </Button>
            </div>
          )}
          {historyQuery.hasNextPage ? (
            <Button
              variant="secondary"
              className="w-full"
              disabled={historyQuery.isFetchingNextPage}
              onClick={() => {
                if (!historyQuery.isFetchingNextPage) void historyQuery.fetchNextPage()
              }}
            >
              {historyQuery.isFetchingNextPage ? t('history.loadingMore') : t('history.loadMore')}
            </Button>
          ) : (
            <p className="text-center text-sm text-muted">{t('history.end')}</p>
          )}
        </div>
      )}
    </section>
  )
}
