import type { OrderStatusLegacy } from '@/shared/models'

/** Valor mínimo (R$) a partir do qual exige aprovação manual. */
export const MANUAL_APPROVAL_THRESHOLD = 5_000

/**
 * Regra de negócio: se TotalAmount > R$ 5.000,00 → status 'Criado' e RequiresManualApproval = true.
 * Se TotalAmount <= R$ 5.000,00 → status 'Pago' e RequiresManualApproval = false.
 */
export function resolveOrderStatus(totalAmount: number): {
  status: OrderStatusLegacy
  requiresManualApproval: boolean
} {
  if (totalAmount > MANUAL_APPROVAL_THRESHOLD) {
    return { status: 'CREATED', requiresManualApproval: true }
  }
  return { status: 'PAID', requiresManualApproval: false }
}

/** Indica se o pedido exige aprovação manual (TotalAmount > 5000). */
export function requiresManualApproval(totalAmount: number): boolean {
  return totalAmount > MANUAL_APPROVAL_THRESHOLD
}
