import { api } from '@/core/api/axios'
import type { OrderListItem, OrdersListResponse, OrderStatus } from '@/shared/models'

/** Valores numéricos enviados na query GET /api/Orders (contrato do backend). */
export const ORDER_STATUS_API = {
  Criado: 0,
  Pago: 1,
  Cancelado: 2,
  Pendente: 3,
} as const

export type OrderStatusApiValue =
  (typeof ORDER_STATUS_API)[keyof typeof ORDER_STATUS_API]

/** Mapeia OrderStatus (UI) para o valor numérico esperado pela API. */
export function orderStatusToApiValue(status: OrderStatus): OrderStatusApiValue {
  return ORDER_STATUS_API[status]
}

/** Parâmetros da query GET /api/Orders (contrato do backend). */
export interface ListOrdersParams {
  pageNumber?: number
  pageSize?: number
  /** Status numérico (0=Criado, 1=Pago, 2=Cancelado, 3=Pendente). */
  status?: OrderStatusApiValue
  /** Data inicial (formato dd/MM/yyyy). */
  dateFrom?: string
  /** Data final (formato dd/MM/yyyy). */
  dateTo?: string
}

/**
 * Formata data yyyy-mm-dd (input date) para dd/MM/yyyy (backend).
 */
export function formatDateForApi(dateYyyyMmDd: string): string {
  if (!dateYyyyMmDd) return ''
  const [y, m, d] = dateYyyyMmDd.split('-')
  if (!y || !m || !d) return dateYyyyMmDd
  return `${d.padStart(2, '0')}/${m.padStart(2, '0')}/${y}`
}

function buildQuery(params: ListOrdersParams): string {
  const search = new URLSearchParams()
  if (params.pageNumber != null) search.set('pageNumber', String(params.pageNumber))
  if (params.pageSize != null) search.set('pageSize', String(params.pageSize))
  if (params.status != null) search.set('status', String(params.status))
  if (params.dateFrom) search.set('dateFrom', params.dateFrom)
  if (params.dateTo) search.set('dateTo', params.dateTo)
  const q = search.toString()
  return q ? `?${q}` : ''
}

const EMPTY_ORDERS_RESPONSE: OrdersListResponse = {
  items: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 20,
}

/** Lista pedidos (GET /api/Orders). Contrato: items, totalCount, pageNumber, pageSize. Em 204, retorna grid vazio. */
export async function listOrders(
  params: ListOrdersParams = {},
): Promise<OrdersListResponse> {
  const response = await api.get<OrdersListResponse>(
    `/orders${buildQuery(params)}`,
    { validateStatus: (status) => status === 200 || status === 204 },
  )
  if (response.status === 204) {
    return {
      ...EMPTY_ORDERS_RESPONSE,
      pageNumber: params.pageNumber ?? 1,
      pageSize: params.pageSize ?? 20,
    }
  }
  return response.data ?? EMPTY_ORDERS_RESPONSE
}

/** Aprova pedido (PUT /api/orders/:id/approve). Retorna o pedido atualizado. */
export async function approveOrder(orderId: string | number): Promise<OrderListItem> {
  const id = typeof orderId === 'number' ? String(orderId) : orderId
  const { data } = await api.put<OrderListItem>(`/orders/${id}/approve`)
  return data
}
