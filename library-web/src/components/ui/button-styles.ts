import { cva } from 'class-variance-authority'

export const buttonVariants = cva(
  'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-55',
  {
    variants: {
      variant: {
        primary: 'bg-primary text-white hover:bg-primary-hover',
        secondary: 'border border-border bg-surface text-ink hover:bg-primary-soft',
        danger: 'bg-danger text-white hover:opacity-90',
        ghost: 'text-primary hover:bg-primary-soft',
      },
      size: {
        default: 'min-h-11',
        small: 'min-h-9 px-3 py-1.5',
      },
    },
    defaultVariants: { variant: 'primary', size: 'default' },
  },
)
