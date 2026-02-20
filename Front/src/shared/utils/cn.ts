import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

/** Combina classes Tailwind sem conflito (tailwind-merge + clsx). */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
