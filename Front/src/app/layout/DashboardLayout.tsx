import type { ReactNode } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { useAuth } from '@/core/auth/use-auth'
import { ROLE_LABELS } from '@/shared/models'
import { ROLES } from '@/shared/models'
import { cn } from '@/shared/utils'
import { getVisibleNavItems } from './dashboard-nav-config'

/** Logo Minerva Foods (sidebar e favicon). */
const MINERVA_LOGO = '/logo-minerva.png'

/** Rota da home por role: MANAGER/ADMIN → dashboard, ANALYST → pedidos. */
function getHomePath(role: string | undefined): string {
  return role === ROLES.MANAGER || role === ROLES.ADMIN ? '/dashboard' : '/orders'
}

interface DashboardLayoutProps {
  children?: ReactNode
}

export function DashboardLayout({ children }: DashboardLayoutProps) {
  const { user, logout } = useAuth()
  const role = user?.role
  const navItems = getVisibleNavItems(role ?? null)
  const homePath = getHomePath(role)

  return (
    <div className="flex min-h-screen bg-slate-50 font-sans">
      <aside className="flex w-64 flex-col bg-minerva-navy">
        <Link
          to={homePath}
          className="flex items-center gap-3 border-b border-white/10 px-4 py-5 transition-colors hover:bg-white/5"
        >
          <div className="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-white p-1 shadow-md">
            <img
              src={MINERVA_LOGO}
              alt="Minerva Foods"
              className="h-full w-full object-contain"
            />
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-white">
              Minerva Foods
            </p>
            <p className="text-xs text-white/70">Gestão de Pedidos</p>
          </div>
        </Link>

        <nav className="flex-1 px-3 py-4 text-sm">
          {navItems.map((item) => {
            const Icon = item.icon
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-2 rounded-md px-3 py-2.5 text-white/90 transition-colors hover:bg-white/10 hover:text-white',
                    isActive &&
                      'bg-minerva-blue text-white hover:bg-minerva-blue',
                  )
                }
              >
                <Icon className="h-4 w-4 shrink-0" />
                <span>{item.label}</span>
              </NavLink>
            )
          })}
        </nav>

        <div className="border-t border-white/10 px-3 py-4">
          {role && (
            <p className="mb-2 px-3 py-1.5 text-xs font-medium text-white/60">
              {ROLE_LABELS[role]}
            </p>
          )}
          <button
            type="button"
            onClick={logout}
            className="flex w-full items-center justify-between rounded-md px-3 py-2 text-xs text-white/80 transition-colors hover:bg-white/10 hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue focus-visible:ring-offset-2 focus-visible:ring-offset-minerva-navy"
          >
            <span>Sair</span>
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </aside>

      <main className="flex-1">
        <div className="mx-auto max-w-6xl px-6 py-8">
          {children ?? <Outlet />}
        </div>
      </main>
    </div>
  )
}
