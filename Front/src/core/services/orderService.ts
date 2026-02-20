import { api } from '@/core/api/axios'
import type {
  CreateOrderRequest,
  CreateOrderResponse,
} from '@/shared/models'

/** Cria um novo pedido (POST /api/orders). O token JWT é enviado pelo interceptor. */
export async function createOrder(
  payload: CreateOrderRequest,
): Promise<CreateOrderResponse> {
  const { data } = await api.post<CreateOrderResponse>('/orders', payload)
  return data
}
