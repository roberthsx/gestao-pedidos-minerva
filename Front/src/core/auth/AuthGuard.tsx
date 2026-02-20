import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/core/auth/use-auth'

interface AuthGuardProps {
  children: ReactNode
}

/** Protege rotas que exigem autenticação; redireciona para /login. */
export function AuthGuard({ children }: AuthGuardProps) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <>{children}</>
}
