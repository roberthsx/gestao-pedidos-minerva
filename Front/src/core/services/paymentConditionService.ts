import { api } from '@/core/api/axios'

export interface PaymentConditionOption {
  id: number
  description: string
  numberOfInstallments: number
}

/** Lista condições de pagamento para select (GET /api/payment-conditions). */
export async function listPaymentConditions(): Promise<
  PaymentConditionOption[]
> {
  const { data } = await api.get<PaymentConditionOption[]>(
    '/payment-conditions',
  )
  return data
}
