import { z } from 'zod'

const orderItemSchema = z.object({
  productName: z
    .string()
    .min(3, 'Nome do produto deve ter no mínimo 3 caracteres'),
  quantity: z.coerce.number().min(1, 'Quantidade mínima é 1'),
  unitPrice: z.coerce.number().gt(0, 'Preço unitário deve ser maior que zero'),
})

export const createOrderFormSchema = z.object({
  customerId: z.coerce
    .number()
    .min(1, 'Selecione o cliente'),
  paymentConditionId: z.coerce
    .number()
    .min(1, 'Selecione a condição de pagamento'),
  orderDate: z.string().min(1, 'Data do pedido é obrigatória'),
  items: z.array(orderItemSchema).min(1, 'Adicione pelo menos um item ao pedido'),
})

export type CreateOrderFormValues = {
  customerId: number
  paymentConditionId: number
  orderDate: string
  items: Array<{ productName: string; quantity: number; unitPrice: number }>
}
