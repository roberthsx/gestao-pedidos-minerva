import axios from 'axios'
import { toast } from 'react-toastify'
import {
  getAuthToken,
  TOAST_MESSAGES,
  toastMessageWithCorrelation,
} from './constants'
import {
  getOrCreateCorrelationId,
  getCurrentCorrelationId,
  generateGuid,
  TRACKING_HEADERS,
  attachCorrelationToError,
  logWithCorrelation,
} from './correlation'

const baseURL =
  typeof import.meta.env.VITE_API_URL !== 'undefined' &&
  import.meta.env.VITE_API_URL !== ''
    ? import.meta.env.VITE_API_URL
    : '/api/v1'

const TIMEOUT_MS = 10_000

export const api = axios.create({
  baseURL,
  timeout: TIMEOUT_MS,
})

api.interceptors.request.use((config) => {
  const token = getAuthToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  config.headers[TRACKING_HEADERS.CORRELATION] = getOrCreateCorrelationId()
  config.headers[TRACKING_HEADERS.CAUSATION] = generateGuid()
  return config
})

let onUnauthorized: (() => Promise<boolean>) | null = null
export function setOnUnauthorized(callback: (() => Promise<boolean>) | null) {
  onUnauthorized = callback
}

function isLoginRequest(config: { url?: string; baseURL?: string } | undefined): boolean {
  if (!config?.url) return false
  const full = (config.baseURL ?? '') + config.url
  return /\/Auth\/login$/i.test(full) || /\/auth\/login$/i.test(full)
}

function showErrorToast(message: string): void {
  const withId = toastMessageWithCorrelation(message, getCurrentCorrelationId())
  if (typeof toast !== 'undefined') toast.error(withId, { autoClose: 6000 })
}

export function markErrorAsHandledByInterceptor(error: unknown): void {
  if (error && typeof error === 'object') {
    (error as { _toastHandled?: boolean })._toastHandled = true
  }
}

export function wasErrorHandledByInterceptor(error: unknown): boolean {
  return Boolean(error && typeof error === 'object' && (error as { _toastHandled?: boolean })._toastHandled)
}

function unwrapEnvelope(response: { data?: unknown }): unknown {
  const body = response.data
  if (body != null && typeof body === 'object' && 'success' in body && (body as { success?: boolean }).success === true && 'data' in body)
    return (body as { data: unknown }).data
  return body
}

api.interceptors.response.use(
  (response) => {
    response.data = unwrapEnvelope(response) as typeof response.data
    return response
  },
  async (error) => {
    const originalRequest = error.config
    attachCorrelationToError(error)

    logWithCorrelation('error', 'Falha na requisição', {
      status: error.response?.status,
      url: error.config?.url,
      method: error.config?.method,
    })

    switch (error.response?.status) {
      case 503:
      case 500:
        showErrorToast(TOAST_MESSAGES.SERVICE_UNAVAILABLE)
        markErrorAsHandledByInterceptor(error)
        return Promise.reject(error)
      case 401: {
        if (originalRequest._retry || !onUnauthorized) {
          if (!isLoginRequest(originalRequest)) showErrorToast(TOAST_MESSAGES.SESSION_EXPIRED)
          return Promise.reject(error)
        }
        originalRequest._retry = true
        try {
          const retry = await onUnauthorized()
          if (retry) return api.request(originalRequest)
        } catch (refreshError) {
          logWithCorrelation('error', 'Falha ao tentar renovar sessão (onUnauthorized)', refreshError)
          if (error && typeof error === 'object') {
            (error as { refreshFailure?: unknown }).refreshFailure = refreshError
          }
        }
        if (!isLoginRequest(originalRequest)) showErrorToast(TOAST_MESSAGES.SESSION_EXPIRED)
        return Promise.reject(error)
      }
      default:
        break
    }

    if (error.code === 'ECONNABORTED') {
      showErrorToast(TOAST_MESSAGES.CONNECTION_TIMEOUT)
      markErrorAsHandledByInterceptor(error)
      return Promise.reject(error)
    }

    if (!error.response && (error.message === 'Network Error' || error.code === 'ERR_NETWORK')) {
      showErrorToast(TOAST_MESSAGES.SERVICE_UNAVAILABLE)
      markErrorAsHandledByInterceptor(error)
      return Promise.reject(error)
    }

    return Promise.reject(error)
  },
)
