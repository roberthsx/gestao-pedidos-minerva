import { api } from '@/core/api/axios'

export interface CustomerOption {
  id: number
  name: string
}

/** Lista clientes para select (GET /api/customers). */
export async function listCustomers(): Promise<CustomerOption[]> {
  const { data } = await api.get<CustomerOption[]>('/customers')
  return data
}
