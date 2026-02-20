/**
 * Rastreamento de requisições: Correlation-ID (fluxo/sessão) e Causation-ID (por requisição).
 * Facilita suporte técnico e correlação de logs no back-end.
 */

const CORRELATION_HEADER = 'X-Correlation-ID'
const CAUSATION_HEADER = 'X-Causation-ID'

let sessionCorrelationId: string | null = null

/** Gera um Guid (UUID v4) para headers de rastreamento. */
export function generateGuid(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID()
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

/**
 * Retorna o Correlation-ID da sessão atual (mesmo fluxo de tela).
 * Se ainda não existir, cria um e persiste para toda a sessão da página.
 */
export function getOrCreateCorrelationId(): string {
  if (!sessionCorrelationId) {
    sessionCorrelationId = generateGuid()
  }
  return sessionCorrelationId
}

/** Retorna o Correlation-ID atual (null se nenhum foi criado ainda). */
export function getCurrentCorrelationId(): string | null {
  return sessionCorrelationId
}

/**
 * Reinicia o Correlation-ID para o próximo request.
 * Use ao iniciar um novo fluxo (ex.: abrir tela de cadastro) para agrupar apenas as ações daquele fluxo.
 */
export function resetCorrelationId(): void {
  sessionCorrelationId = null
}

/**
 * Define um Correlation-ID específico (ex.: recebido do back-end ou para continuar um fluxo).
 */
export function setCorrelationId(id: string): void {
  sessionCorrelationId = id
}

/** Nomes dos headers para uso no interceptor. */
export const TRACKING_HEADERS = {
  CORRELATION: CORRELATION_HEADER,
  CAUSATION: CAUSATION_HEADER,
} as const

/** Adorna um objeto de erro com o CorrelationID atual para logs. */
export function attachCorrelationToError(error: unknown): void {
  if (error && typeof error === 'object') {
    const id = getCurrentCorrelationId()
    if (id) {
      (error as { correlationId?: string }).correlationId = id
    }
  }
}

/**
 * Log no console incluindo CorrelationID para suporte.
 * Use em catch blocks ou em eventos importantes.
 */
export function logWithCorrelation(
  level: 'error' | 'warn' | 'info' | 'debug',
  message: string,
  ...args: unknown[]
): void {
  const correlationId = getCurrentCorrelationId()
  const prefix = correlationId ? `[CorrelationID: ${correlationId}]` : ''
  const consoleMethod = console[level] ?? console.log
  if (args.length > 0) {
    consoleMethod(`${prefix} ${message}`, ...args)
  } else {
    consoleMethod(`${prefix} ${message}`)
  }
}
