import type { LucideIcon } from 'lucide-react'
import { LayoutDashboard, ListOrdered } from 'lucide-react'
import type { Role } from '@/shared/models'
import { ROLES } from '@/shared/models'

export interface NavItem {
  path: string
  label: string
  icon: LucideIcon
  /** Perfis que podem ver este item. */
  allowedRoles: Role[]
}

export const DASHBOARD_NAV_ITEMS: NavItem[] = [
  {
    path: '/dashboard',
    label: 'Dashboard',
    icon: LayoutDashboard,
    allowedRoles: [ROLES.MANAGER, ROLES.ADMIN],
  },
  {
    path: '/orders',
    label: 'Pedidos',
    icon: ListOrdered,
    allowedRoles: [ROLES.MANAGER, ROLES.ADMIN, ROLES.ANALYST],
  },
]

/** Retorna os itens de menu visíveis para o perfil informado. */
export function getVisibleNavItems(role: Role | null): NavItem[] {
  if (!role) return []
  return DASHBOARD_NAV_ITEMS.filter((item) => item.allowedRoles.includes(role))
}
