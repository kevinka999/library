import { Field, FieldArray, Form, Formik, getIn, type FormikHelpers } from 'formik'
import * as Yup from 'yup'
import { useTranslation } from 'react-i18next'
import { Plus, Trash2 } from 'lucide-react'
import { ApiError } from '../api/client'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'
import { Textarea } from './ui/textarea'

export interface BookFormValues {
  title: string
  shortDescription: string
  publishDate: string
  authors: string[]
}

interface BookFormProps {
  initialValues: BookFormValues
  submitLabel: string
  pendingLabel: string
  onSubmit: (values: BookFormValues) => Promise<void>
  onCancel?: () => void
  confirmCancelWhenDirty?: boolean
  onReloadCurrent?: () => void
  onApiError?: (error: ApiError) => void
}

function apiFieldPath(key: string): string | null {
  const normalized = key.replace(/^\$\./, '')
  const firstLower = normalized.charAt(0).toLowerCase() + normalized.slice(1)
  if (/^(title|shortDescription|publishDate|authors)(\[\d+\])?$/.test(firstLower)) {
    return firstLower
  }
  return null
}

export function BookForm({
  initialValues,
  submitLabel,
  pendingLabel,
  onSubmit,
  onCancel,
  confirmCancelWhenDirty = false,
  onReloadCurrent,
  onApiError,
}: BookFormProps) {
  const { t } = useTranslation()
  const schema = Yup.object({
    title: Yup.string().trim().required(t('validation.required')).max(300, t('validation.max', { count: 300 })),
    shortDescription: Yup.string().trim().required(t('validation.required')).max(1000, t('validation.max', { count: 1000 })),
    publishDate: Yup.string().required(t('validation.required')),
    authors: Yup.array()
      .of(Yup.string().trim().required(t('validation.authorRequired')).max(200, t('validation.max', { count: 200 })))
      .min(1, t('validation.authorMinimum'))
      .test('unique', t('validation.authorUnique'), (authors) => {
        if (!authors) return true
        const normalized = authors
          .filter((author): author is string => typeof author === 'string' && author.trim() !== '')
          .map((author) => author.trim().toLocaleLowerCase())
        return new Set(normalized).size === normalized.length
      }),
  })

  async function submit(values: BookFormValues, helpers: FormikHelpers<BookFormValues>) {
    helpers.setStatus(undefined)
    try {
      await onSubmit(values)
    } catch (error) {
      if (error instanceof ApiError && error.kind === 'validation') {
        onApiError?.(error)
        let unmatched = 0
        for (const [key] of Object.entries(error.fieldErrors)) {
          const path = apiFieldPath(key)
          if (path) helpers.setFieldError(path, t('validation.serverField'))
          else unmatched += 1
        }
        if (unmatched > 0 || Object.keys(error.fieldErrors).length === 0) {
          helpers.setStatus('validation')
        }
      } else if (error instanceof ApiError) {
        onApiError?.(error)
        helpers.setStatus(error.kind)
      } else {
        helpers.setStatus('unexpected')
      }
    }
  }

  return (
    <Formik initialValues={initialValues} validationSchema={schema} onSubmit={submit}>
      {({ errors, touched, isSubmitting, status, dirty }) => (
        <Form className="mt-6 space-y-5" noValidate>
          <div>
            <Label htmlFor="title">{t('forms.title')}</Label>
            <Field as={Input} id="title" name="title" maxLength={300} className="mt-1.5" />
            {touched.title && errors.title && <p className="mt-1.5 text-sm text-danger">{errors.title}</p>}
          </div>
          <div>
            <Label htmlFor="shortDescription">{t('forms.shortDescription')}</Label>
            <Field as={Textarea} id="shortDescription" name="shortDescription" maxLength={1000} className="mt-1.5" />
            {touched.shortDescription && errors.shortDescription && (
              <p className="mt-1.5 text-sm text-danger">{errors.shortDescription}</p>
            )}
          </div>
          <div>
            <Label htmlFor="publishDate">{t('forms.publishDate')}</Label>
            <Field as={Input} id="publishDate" name="publishDate" type="date" className="mt-1.5 max-w-xs" />
            {touched.publishDate && errors.publishDate && (
              <p className="mt-1.5 text-sm text-danger">{errors.publishDate}</p>
            )}
          </div>
          <FieldArray name="authors">
            {({ form, push, remove }) => (
              <fieldset>
                <legend className="text-sm font-semibold text-ink">{t('forms.authors')}</legend>
                <div className="mt-2 space-y-3">
                  {form.values.authors.map((_: string, index: number) => {
                    const path = `authors[${index}]`
                    const error = getIn(form.errors, path) as string | undefined
                    const wasTouched = getIn(form.touched, path) as boolean | undefined
                    return (
                      <div key={index}>
                        <div className="flex items-start gap-2">
                          <div className="grow">
                            <Label className="sr-only" htmlFor={`author-${index}`}>
                              {t('forms.authorNumber', { number: index + 1 })}
                            </Label>
                            <Field
                              as={Input}
                              id={`author-${index}`}
                              name={path}
                              maxLength={200}
                              placeholder={t('forms.authorPlaceholder')}
                            />
                          </div>
                          <Button
                            type="button"
                            variant="ghost"
                            size="small"
                            className="mt-1"
                            aria-label={t('forms.removeAuthor', { number: index + 1 })}
                            disabled={form.values.authors.length === 1}
                            onClick={() => remove(index)}
                          >
                            <Trash2 size={18} aria-hidden="true" />
                          </Button>
                        </div>
                        {wasTouched && error && <p className="mt-1.5 text-sm text-danger">{error}</p>}
                      </div>
                    )
                  })}
                </div>
                {typeof errors.authors === 'string' && <p className="mt-1.5 text-sm text-danger">{errors.authors}</p>}
                <Button type="button" variant="secondary" size="small" className="mt-3" onClick={() => push('')}>
                  <Plus size={17} aria-hidden="true" />
                  {t('forms.addAuthor')}
                </Button>
              </fieldset>
            )}
          </FieldArray>
          {status && (
            <div role="alert" className="rounded-lg bg-danger-soft p-3 text-sm text-danger">
              <p>{t(`errors.${status === 'validation' ? 'validation' : status}`)}</p>
              {status === 'stale' && onReloadCurrent && (
                <Button
                  type="button"
                  variant="secondary"
                  size="small"
                  className="mt-3"
                  onClick={() => {
                    if (!dirty || window.confirm(t('forms.reloadConfirm'))) onReloadCurrent()
                  }}
                >
                  {t('forms.reloadCurrent')}
                </Button>
              )}
            </div>
          )}
          <div className="flex flex-col-reverse gap-3 border-t border-border pt-5 sm:flex-row sm:justify-end">
            {onCancel && (
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  if (!confirmCancelWhenDirty || !dirty || window.confirm(t('forms.discardConfirm'))) onCancel()
                }}
                disabled={isSubmitting}
              >
                {t('common.cancel')}
              </Button>
            )}
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? pendingLabel : submitLabel}
            </Button>
          </div>
        </Form>
      )}
    </Formik>
  )
}
