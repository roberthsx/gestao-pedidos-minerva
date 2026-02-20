/** Interface baseada na modelagem: entidade OrderItem. */
export interface OrderItem {
  id: string
  orderId: string
  productId: string
  quantity: number
  unitPrice: number
  totalPrice: number
}
