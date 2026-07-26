import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useCreateBook } from '../../../api/books/hooks'
import { BookForm, type BookFormValues } from '../../../components/book-form'
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '../../../components/ui/dialog'

const emptyBook: BookFormValues = {
  title: '',
  shortDescription: '',
  publishDate: '',
  authors: [''],
}

interface CreateBookDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateBookDialog({ open, onOpenChange }: CreateBookDialogProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createMutation = useCreateBook()

  async function create(values: BookFormValues) {
    const result = await createMutation.mutateAsync(values)
    onOpenChange(false)
    await navigate(`/books/${result.book.id}`)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogTitle>{t('forms.createTitle')}</DialogTitle>
        <DialogDescription>{t('forms.createDescription')}</DialogDescription>
        <BookForm
          initialValues={emptyBook}
          submitLabel={t('forms.create')}
          pendingLabel={t('forms.creating')}
          onSubmit={create}
          onCancel={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}
