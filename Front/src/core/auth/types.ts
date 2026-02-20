import type { User } from '@/shared/models'

export interface AuthContextValue {
  user: User | null
  isAuthenticated: boolean
  /** true enquanto valida o token do localStorage (evita redirect para login no F5). */
  loading: boolean
  /** Realiza login e retorna o user (para redirecionamento por perfil). */
  login: (registrationNumber: string, password: string) => Promise<{ user: User }>
  logout: () => void
}
