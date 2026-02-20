/** Contrato do body da requisição POST /api/orders (criar pedido). */
export interface CreateOrderItemRequest {
  productName: string
  quantity: number
  unitPrice: number
}

export interface CreateOrderRequest {
  customerId: number
  paymentConditionId: number
  orderDate: string
  items: CreateOrderItemRequest[]
}

/** Contrato da resposta 201 Created POST /api/orders. */
export interface CreateOrderItemResponse {
  productName: string
  quantity: number
  unitPrice: number
  totalPrice: number
}

export interface CreateOrderResponse {
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
  items: CreateOrderItemResponse[]
}
