import { api } from '@/core/api/axios'
import type { LoginResponse } from '@/shared/models'

/** Login (contrato V1: registrationNumber, senha; resposta user.name, user.role). */
export async function loginWithApi(
  registrationNumber: string,
  password: string,
): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/Auth/login', {
    registrationNumber: registrationNumber.trim(),
    senha: password,
  })
  return data
}
