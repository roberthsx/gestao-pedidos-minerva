import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import { AUTH_STORAGE_KEY, getStoredAuth } from '@/core/api/constants'
import { setOnUnauthorized } from '@/core/api/axios'
import { login as loginApi } from '@/core/services/authService'
import type { AuthContextValue } from '@/core/auth/types'
import type { User } from '@/shared/models'

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

/** Lê user do localStorage de forma síncrona para o estado inicial (rehidratação no F5). */
function getInitialUser(): User | null {
  const stored = getStoredAuth()
  if (!stored?.accessToken || !stored?.user) return null
  return {
    name: stored.user.name,
    role: stored.user.role as User['role'],
  }
}

function persistAuth(
  accessToken: string,
  expiresIn: number,
  user: User,
): void {
  const expiresAt = Date.now() + expiresIn * 1000
  const payload = {
    accessToken,
    expiresIn,
    user,
    expiresAt,
  }
  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(payload))
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(getInitialUser)
  const [loading, setLoading] = useState(true)
  const credentialsRef = useRef<{ matricula: string; senha: string } | null>(null)
  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const clearRefreshTimer = useCallback(() => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current)
      refreshTimerRef.current = null
    }
  }, [])

  const handleSilentRefresh = useCallback(async (): Promise<boolean> => {
    const creds = credentialsRef.current
    if (!creds) return false
    try {
      const res = await loginApi(creds.matricula, creds.senha)
      persistAuth(res.accessToken, res.expiresIn, res.user)
      setUser(res.user)
      scheduleRefresh(res.expiresIn)
      return true
    } catch {
      credentialsRef.current = null
      window.localStorage.removeItem(AUTH_STORAGE_KEY)
      setUser(null)
      clearRefreshTimer()
      return false
    }
  }, [clearRefreshTimer])

  const scheduleRefresh = useCallback(
    (expiresInSeconds: number) => {
      clearRefreshTimer()
      const msUntilRefresh = Math.max(0, (expiresInSeconds - 300) * 1000)
      refreshTimerRef.current = setTimeout(() => {
        refreshTimerRef.current = null
        void handleSilentRefresh()
      }, msUntilRefresh)
    },
    [clearRefreshTimer, handleSilentRefresh],
  )

  useEffect(() => {
    const stored = getStoredAuth()
    if (!stored?.accessToken || !stored?.user) {
      setLoading(false)
      return
    }
    const now = Date.now()
    if (stored.expiresAt <= now) {
      window.localStorage.removeItem(AUTH_STORAGE_KEY)
      setUser(null)
      setLoading(false)
      return
    }
    const userFromStorage: User = {
      name: stored.user.name,
      role: stored.user.role as User['role'],
    }
    setUser(userFromStorage)
    const remainingSeconds = Math.max(0, (stored.expiresAt - now) / 1000)
    if (remainingSeconds > 300) {
      scheduleRefresh(remainingSeconds)
    } else {
      void handleSilentRefresh().finally(() => setLoading(false))
      return
    }
    setLoading(false)
  }, [scheduleRefresh, handleSilentRefresh])

  useEffect(() => {
    setOnUnauthorized(handleSilentRefresh)
    return () => setOnUnauthorized(null)
  }, [handleSilentRefresh])

  const login = useCallback(
    async (registrationNumber: string, password: string): Promise<{ user: User }> => {
      const res = await loginApi(registrationNumber, password)
      persistAuth(res.accessToken, res.expiresIn, res.user)
      setUser(res.user)
      credentialsRef.current = { matricula: registrationNumber.trim(), senha: password }
      scheduleRefresh(res.expiresIn)
      return { user: res.user }
    },
    [scheduleRefresh],
  )

  const logout = useCallback(() => {
    credentialsRef.current = null
    clearRefreshTimer()
    window.localStorage.removeItem(AUTH_STORAGE_KEY)
    setUser(null)
  }, [clearRefreshTimer])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      loading,
      login,
      logout,
    }),
    [user, loading, login, logout],
  )

  return (
    <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth deve ser usado dentro de AuthProvider')
  }
  return ctx
}
