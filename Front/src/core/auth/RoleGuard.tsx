import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/core/auth/use-auth'
import { AccessDenied } from '@/core/auth/AccessDenied'

interface RoleGuardProps {
  children: ReactNode
  /** Lista de perfis permitidos (ex.: ['MANAGER']). Se vazio, só exige autenticação. */
  allowedRoles: string[]
  /** Se true, exibe tela "Acesso Negado" em vez de redirecionar para /orders. */
  showAccessDenied?: boolean
}

/**
 * Protege rotas por perfil: exige autenticação e que o role do usuário esteja em allowedRoles.
 * Se não autenticado → redireciona para /login.
 * Se role não permitido → redireciona para /orders ou exibe tela "Acesso Negado".
 */
export function RoleGuard({
  children,
  allowedRoles,
  showAccessDenied = true,
}: RoleGuardProps) {
  const { isAuthenticated, user } = useAuth()
  const location = useLocation()

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  const hasAllowedRole =
    allowedRoles.length > 0 && allowedRoles.includes(user.role)

  if (!hasAllowedRole) {
    if (showAccessDenied) return <AccessDenied />
    return <Navigate to="/orders" replace />
  }

  return <>{children}</>
}
