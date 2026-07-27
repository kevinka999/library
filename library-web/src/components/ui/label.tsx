import type { LabelHTMLAttributes } from 'react'
import * as LabelPrimitive from '@radix-ui/react-label'
import { cn } from '../../lib/utils'

export function Label({ className, ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return <LabelPrimitive.Root className={cn('block text-sm font-semibold text-ink', className)} {...props} />
}
