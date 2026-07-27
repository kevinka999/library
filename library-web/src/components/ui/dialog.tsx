import type { ComponentProps } from 'react'
import * as DialogPrimitive from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '../../lib/utils'

export function Dialog(props: ComponentProps<typeof DialogPrimitive.Root>) {
  return <DialogPrimitive.Root {...props} />
}

export function DialogContent({ className, children, ...props }: ComponentProps<typeof DialogPrimitive.Content>) {
  const { t } = useTranslation()
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="fixed inset-0 z-40 bg-ink/55 backdrop-blur-[2px]" />
      <DialogPrimitive.Content
        className={cn('fixed left-1/2 top-1/2 z-50 max-h-[90vh] w-[calc(100%-2rem)] max-w-2xl -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-2xl border border-border bg-surface p-5 shadow-2xl sm:p-7', className)}
        {...props}
      >
        {children}
        <DialogPrimitive.Close
          className="absolute right-4 top-4 grid size-10 place-items-center rounded-lg text-muted hover:bg-primary-soft hover:text-primary"
          aria-label={t('common.close')}
        >
          <X size={20} />
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  )
}

export function DialogTitle({ className, ...props }: ComponentProps<typeof DialogPrimitive.Title>) {
  return <DialogPrimitive.Title className={cn('pr-12 font-serif text-3xl font-semibold', className)} {...props} />
}

export function DialogDescription({ className, ...props }: ComponentProps<typeof DialogPrimitive.Description>) {
  return <DialogPrimitive.Description className={cn('mt-2 text-sm text-muted', className)} {...props} />
}
