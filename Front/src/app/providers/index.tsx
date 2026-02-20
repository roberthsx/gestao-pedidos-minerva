import type { ReactNode } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { ToastContainer } from 'react-toastify'
import { queryClient } from '@/core/api/query-client'
import { AuthProvider } from '@/core/auth/use-auth'
import 'react-toastify/dist/ReactToastify.css'

interface AppProvidersProps {
  children: ReactNode
}

/** Agrupa TanStack Query, AuthContext, React Router e toasts globais. */
export function AppProviders({ children }: AppProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>{children}</BrowserRouter>
        <ToastContainer
          position="top-right"
          autoClose={6000}
          hideProgressBar={false}
          newestOnTop
          closeOnClick
          rtl={false}
          pauseOnFocusLoss
          pauseOnHover
          theme="light"
        />
      </AuthProvider>
    </QueryClientProvider>
  )
}
