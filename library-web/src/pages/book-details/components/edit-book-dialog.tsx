import { useTranslation } from 'react-i18next'
import { useUpdateBook } from '../../../api/books/hooks'
import type { ApiError } from '../../../api/client'
import { BookForm, type BookFormValues } from '../../../components/book-form'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from '../../../components/ui/dialog'
import type { BookWithEtag } from '../../../types/book'

interface EditBookDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  initialBook: BookWithEtag
  onReloadCurrent: () => Promise<boolean>
  onNotFound: () => void
}

export function EditBookDialog({
  open,
  onOpenChange,
  initialBook,
  onReloadCurrent,
  onNotFound,
}: EditBookDialogProps) {
  const { t } = useTranslation()
  const updateMutation = useUpdateBook()
  const { book, etag } = initialBook
  const initialValues: BookFormValues = {
    title: book.title,
    shortDescription: book.shortDescription,
    publishDate: book.publishDate,
    authors: [...book.authors],
  }

  function close() {
    onOpenChange(false)
  }

  async function reloadCurrent() {
    if (await onReloadCurrent()) close()
  }

  function handleApiError(error: ApiError) {
    if (error.kind === 'not-found') onNotFound()
  }

  async function update(values: BookFormValues) {
    await updateMutation.mutateAsync({
      id: book.id,
      etag,
      ...values,
    })
    close()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogTitle>{t('forms.editTitle')}</DialogTitle>
        <DialogDescription>{t('forms.editDescription')}</DialogDescription>
        <BookForm
          key={`${initialBook.book.id}:${initialBook.etag}`}
          initialValues={initialValues}
          submitLabel={t('forms.save')}
          pendingLabel={t('forms.saving')}
          onSubmit={update}
          onCancel={close}
          onReloadCurrent={reloadCurrent}
          onApiError={handleApiError}
        />
      </DialogContent>
    </Dialog>
  )
}
