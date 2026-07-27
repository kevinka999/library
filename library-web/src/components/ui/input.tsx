import type { InputHTMLAttributes } from 'react'
import { cn } from '../../lib/utils'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn('min-h-11 w-full rounded-lg border border-border bg-surface px-3 text-ink shadow-sm placeholder:text-muted/70 disabled:opacity-60', className)}
      {...props}
    />
  )
}
