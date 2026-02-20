import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { cn } from '@/shared/utils'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'success'
type ButtonSize = 'sm' | 'md'

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  children: ReactNode
}

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    'bg-minerva-blue text-white hover:bg-[#005ba8] disabled:bg-slate-300 disabled:text-slate-500 focus-visible:ring-minerva-blue',
  secondary:
    'bg-white text-slate-900 border border-slate-300 hover:bg-slate-50 disabled:bg-slate-100 disabled:text-slate-400 focus-visible:ring-slate-400',
  ghost:
    'bg-transparent text-slate-700 hover:bg-slate-100 disabled:text-slate-400 focus-visible:ring-slate-400',
  success:
    'bg-minerva-green text-minerva-navy hover:bg-[#7ab838] disabled:bg-slate-300 disabled:text-slate-500 focus-visible:ring-minerva-green font-semibold',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'px-3 py-1.5 text-xs',
  md: 'px-4 py-2 text-sm',
}

export function Button({
  variant = 'primary',
  size = 'md',
  className,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  )
}
