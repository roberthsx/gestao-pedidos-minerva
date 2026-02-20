/** Interfaces baseadas na modelagem: entidade Order. */

/** Status no contrato legado (inglês). */
export type OrderStatusLegacy = 'CREATED' | 'PAID' | 'CANCELLED'

/** Status de pedido para UI e filtro (contrato back-end em PT). */
export type OrderStatus = 'Pendente' | 'Criado' | 'Pago' | 'Cancelado'

/** Opções do Select de filtro por status (ordem de exibição). */
export const ORDER_STATUS_OPTIONS: OrderStatus[] = [
  'Pendente',
  'Criado',
  'Pago',
  'Cancelado',
]

export interface Order {
  id: string
  customerId: string
  customerName: string
  totalAmount: number
  status: OrderStatusLegacy
  requiresManualApproval: boolean
  createdAt?: string
}

/** Item de pedido dentro do pedido (GET /api/Orders). */
export interface OrderItemDto {
  productName: string
  quantity: number
  unitPrice: number
  totalPrice: number
}

/** Item da listagem paginada (contrato GET /api/Orders). */
export interface OrderListItem {
  id: number
  customerId: number
  customerName: string
  paymentConditionId: number
  paymentConditionDescription: string
  orderDate: string
  totalAmount: number
  status: string
  requiresManualApproval: boolean
  deliveryDays: number
  estimatedDeliveryDate: string
  items: OrderItemDto[]
}

/** Resposta paginada GET /api/Orders. */
export interface OrdersListResponse {
  items: OrderListItem[]
  totalCount: number
  pageNumber: number
  pageSize: number
}
