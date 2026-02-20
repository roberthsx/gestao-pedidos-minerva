import { api } from '@/core/api/axios'
import type { LoginResponse } from '@/shared/models'

const AUTH_LOGIN_PATH = '/Auth/login'

/**
 * Realiza login na API. Envia "senha" no JSON (back-end aceita para compatibilidade).
 * Retorna accessToken, expiresIn e user (name, role).
 */
export async function login(
  registrationNumber: string,
  password: string,
): Promise<LoginResponse> {
  const payload = {
    registrationNumber: registrationNumber.trim(),
    senha: password,
  }
  const { data } = await api.post<LoginResponse>(AUTH_LOGIN_PATH, payload)
  return data
}
