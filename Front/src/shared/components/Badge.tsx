import type { HTMLAttributes, ReactNode } from 'react'
import { cn } from '@/shared/utils'

type BadgeVariant = 'default' | 'success' | 'warning' | 'destructive'

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant
  children: ReactNode
}

const variantClasses: Record<BadgeVariant, string> = {
  default:
    'bg-minerva-blue-light text-minerva-blue border-minerva-blue/30',
  success:
    'bg-minerva-green/20 text-[#2d5a0f] border-minerva-green/50',
  warning:
    'bg-minerva-blue-light text-minerva-blue border-minerva-blue/40',
  destructive: 'bg-red-50 text-red-800 border-red-600/30',
}

export function Badge({
  variant = 'default',
  className,
  children,
  ...props
}: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center justify-center rounded-full border px-2.5 py-0.5 text-xs font-medium',
        variantClasses[variant],
        className,
      )}
      {...props}
    >
      {children}
    </span>
  )
}
