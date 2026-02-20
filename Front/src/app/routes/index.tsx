import { Route, Routes, Navigate } from 'react-router-dom'
import { ProtectedRoute } from '@/core/auth'
import { useAuth } from '@/core/auth/use-auth'
import { ROLES } from '@/shared/models'
import { LoginPage } from '@/features/auth/LoginPage'
import { Dashboard } from '@/features/dashboard/Dashboard'
import { OrdersListPage } from '@/features/orders/OrdersListPage'
import { OrderCreatePage } from '@/features/orders/OrderCreatePage'
import { DashboardLayout } from '@/app/layout/DashboardLayout'

export function AppRoutes() {
  const { isAuthenticated, user } = useAuth()

  const defaultRedirect = !isAuthenticated
    ? '/login'
    : user?.role === ROLES.MANAGER || user?.role === ROLES.ADMIN
      ? '/dashboard'
      : '/orders'

  return (
    <Routes>
      <Route path="/" element={<Navigate to={defaultRedirect} replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute allowedRoles={[ROLES.MANAGER, ROLES.ADMIN]}>
            <DashboardLayout>
              <Dashboard />
            </DashboardLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/orders"
        element={
          <ProtectedRoute>
            <DashboardLayout>
              <OrdersListPage />
            </DashboardLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/orders/new"
        element={
          <ProtectedRoute allowedRoles={[ROLES.MANAGER, ROLES.ADMIN]}>
            <DashboardLayout>
              <OrderCreatePage />
            </DashboardLayout>
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
