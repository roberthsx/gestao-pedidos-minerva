import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/core/auth/use-auth'
import { AuthLoadingScreen } from '@/core/auth/AuthLoadingScreen'
import { AccessDenied } from '@/core/auth/AccessDenied'
import type { Role } from '@/shared/models'

export interface ProtectedRouteProps {
  children: ReactNode
  /** Perfis permitidos (RBAC). Se não informado, apenas exige autenticação. */
  allowedRoles?: Role[]
  /** Exibir tela "Acesso Negado" em vez de redirecionar quando o perfil não é permitido. */
  showAccessDenied?: boolean
}

/**
 * Protege a rota: exige usuário logado e, se allowedRoles for informado,
 * verifica se user.role está em allowedRoles (RBAC).
 * Enquanto loading (validação do token no F5), exibe splash em vez de redirecionar.
 */
export function ProtectedRoute({
  children,
  allowedRoles,
  showAccessDenied = true,
}: ProtectedRouteProps) {
  const { isAuthenticated, user, loading } = useAuth()
  const location = useLocation()

  if (loading) {
    return <AuthLoadingScreen />
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (allowedRoles != null && allowedRoles.length > 0) {
    const hasRole = allowedRoles.includes(user.role)
    if (!hasRole) {
      if (showAccessDenied) return <AccessDenied />
      return <Navigate to="/orders" replace />
    }
  }

  return <>{children}</>
}
