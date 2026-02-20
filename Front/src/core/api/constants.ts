/** Chave de persistência da resposta completa de login (localStorage). */
export const AUTH_STORAGE_KEY = '@Minerva:Auth'

/** Mensagens centralizadas para toasts de erro (rastreabilidade e suporte). */
export const TOAST_MESSAGES = {
  SERVICE_UNAVAILABLE:
    'Serviço indisponível no momento. Nossa equipe já está trabalhando nisso. Tente novamente em instantes.',
  SESSION_EXPIRED: 'Sessão expirada ou não autorizado. Faça login novamente.',
  CONNECTION_TIMEOUT: 'A conexão demorou muito a responder. Verifique sua internet.',
} as const

/** Retorna mensagem de toast com Correlation-ID de forma discreta (suporte técnico). */
export function toastMessageWithCorrelation(
  message: string,
  correlationId: string | null,
): string {
  if (!correlationId) return message
  return `${message} — ID: ${correlationId}`
}

/** Retorna o accessToken armazenado em @Minerva:Auth (usado pelo interceptor Axios). */
export function getAuthToken(): string | null {
  if (typeof window === 'undefined') return null
  try {
    const raw = window.localStorage.getItem(AUTH_STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as { accessToken?: string }
    return parsed.accessToken ?? null
  } catch {
    return null
  }
}

/** Retorna o objeto completo de auth do localStorage (para hidratação do contexto). */
export function getStoredAuth(): StoredAuth | null {
  if (typeof window === 'undefined') return null
  try {
    const raw = window.localStorage.getItem(AUTH_STORAGE_KEY)
    if (!raw) return null
    return JSON.parse(raw) as StoredAuth
  } catch {
    return null
  }
}

export interface StoredAuth {
  accessToken: string
  expiresIn: number
  user: { name: string; role: string }
  /** Timestamp (ms) em que o token expira. */
  expiresAt: number
}
