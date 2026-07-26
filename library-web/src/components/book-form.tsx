import { getIn, useFormik } from 'formik'
import { useState } from 'react'
import * as Yup from 'yup'
import { useTranslation } from 'react-i18next'
import { Plus, Trash2 } from 'lucide-react'
import { ApiError, type ApiErrorKind } from '../api/client'
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
  onReloadCurrent?: () => void | Promise<void>
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
  onReloadCurrent,
  onApiError,
}: BookFormProps) {
  const [submitError, setSubmitError] = useState<ApiErrorKind | null>(null)
  const { t } = useTranslation()
  const formik = useFormik<BookFormValues>({
    initialValues,
    validationSchema: Yup.object({
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
    }),
    onSubmit: submit,
  })

  async function submit(values: BookFormValues) {
    setSubmitError(null)

    try {
      await onSubmit(values)
    } catch (error) {
      if (error instanceof ApiError && error.kind === 'validation') {
        onApiError?.(error)
        let unmatched = 0

        for (const [key] of Object.entries(error.fieldErrors)) {
          const path = apiFieldPath(key)
          if (path) formik.setFieldError(path, t('validation.serverField'))
          else unmatched += 1
        }

        if (unmatched > 0 || Object.keys(error.fieldErrors).length === 0) {
          setSubmitError('validation')
        }
      } else if (error instanceof ApiError) {
        onApiError?.(error)
        setSubmitError(error.kind)
      } else {
        setSubmitError('unexpected')
      }
    }
  }

  function addAuthor() {
    void formik.setFieldValue('authors', [...formik.values.authors, ''])
  }

  function removeAuthor(index: number) {
    if (formik.values.authors.length === 1) return
    void formik.setFieldValue(
      'authors',
      formik.values.authors.filter((_, authorIndex) => authorIndex !== index),
    )
  }

  return (
    <form className="mt-6 space-y-5" noValidate onSubmit={formik.handleSubmit}>
      <div>
        <Label htmlFor="title">{t('forms.title')}</Label>
        <Input
          id="title"
          maxLength={300}
          className="mt-1.5"
          {...formik.getFieldProps('title')}
        />
        {formik.touched.title && formik.errors.title && (
          <p className="mt-1.5 text-sm text-danger">{formik.errors.title}</p>
        )}
      </div>
      <div>
        <Label htmlFor="shortDescription">{t('forms.shortDescription')}</Label>
        <Textarea
          id="shortDescription"
          maxLength={1000}
          className="mt-1.5"
          {...formik.getFieldProps('shortDescription')}
        />
        {formik.touched.shortDescription && formik.errors.shortDescription && (
          <p className="mt-1.5 text-sm text-danger">{formik.errors.shortDescription}</p>
        )}
      </div>
      <div>
        <Label htmlFor="publishDate">{t('forms.publishDate')}</Label>
        <Input
          id="publishDate"
          type="date"
          className="mt-1.5 max-w-xs"
          {...formik.getFieldProps('publishDate')}
        />
        {formik.touched.publishDate && formik.errors.publishDate && (
          <p className="mt-1.5 text-sm text-danger">{formik.errors.publishDate}</p>
        )}
      </div>
      <fieldset>
        <legend className="text-sm font-semibold text-ink">{t('forms.authors')}</legend>
        <div className="mt-2 space-y-3">
          {formik.values.authors.map((_, index) => {
            const path = `authors[${index}]`
            const error = getIn(formik.errors, path) as string | undefined
            const wasTouched = getIn(formik.touched, path) as boolean | undefined

            return (
              <div key={index}>
                <div className="flex items-start gap-2">
                  <div className="grow">
                    <Label className="sr-only" htmlFor={`author-${index}`}>
                      {t('forms.authorNumber', { number: index + 1 })}
                    </Label>
                    <Input
                      id={`author-${index}`}
                      maxLength={200}
                      placeholder={t('forms.authorPlaceholder')}
                      {...formik.getFieldProps(path)}
                    />
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="small"
                    className="mt-1"
                    aria-label={t('forms.removeAuthor', { number: index + 1 })}
                    disabled={formik.values.authors.length === 1}
                    onClick={() => removeAuthor(index)}
                  >
                    <Trash2 size={18} aria-hidden="true" />
                  </Button>
                </div>
                {wasTouched && error && <p className="mt-1.5 text-sm text-danger">{error}</p>}
              </div>
            )
          })}
        </div>
        {typeof formik.errors.authors === 'string' && (
          <p className="mt-1.5 text-sm text-danger">{formik.errors.authors}</p>
        )}
        <Button type="button" variant="secondary" size="small" className="mt-3" onClick={addAuthor}>
          <Plus size={17} aria-hidden="true" />
          {t('forms.addAuthor')}
        </Button>
      </fieldset>
      {submitError && (
        <div role="alert" className="rounded-lg bg-danger-soft p-3 text-sm text-danger">
          <p>{t(`errors.${submitError}`)}</p>
          {submitError === 'stale' && onReloadCurrent && (
            <Button
              type="button"
              variant="secondary"
              size="small"
              className="mt-3"
              onClick={() => void onReloadCurrent()}
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
            onClick={onCancel}
            disabled={formik.isSubmitting}
          >
            {t('common.cancel')}
          </Button>
        )}
        <Button type="submit" disabled={formik.isSubmitting}>
          {formik.isSubmitting ? pendingLabel : submitLabel}
        </Button>
      </div>
    </form>
  )
}
