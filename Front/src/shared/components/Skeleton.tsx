import type { HTMLAttributes } from 'react'
import { cn } from '@/shared/utils'

export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {}

export function Skeleton({ className, ...props }: SkeletonProps) {
  return (
    <div
      className={cn('animate-pulse rounded-md bg-slate-200', className)}
      aria-hidden
      {...props}
    />
  )
}
