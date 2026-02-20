import { Badge, Button, Tooltip } from '@/shared/components'
import { formatCurrency, formatOrderDate } from '@/shared/utils'
import type { OrderListItem } from '@/shared/models'
import { tableStyles } from './order-table-styles'

const BADGE_CLASS = 'w-[5.5rem] min-w-[5.5rem] max-w-[5.5rem]'

/** Normaliza status da API (PT ou EN) para comparação. */
function normalizeStatus(status: string | undefined): 'paid' | 'cancelled' | 'created' | '' {
  const s = (status ?? '').trim().toUpperCase()
  if (s === 'PAID' || s === 'PAGO') return 'paid'
  if (s === 'CANCELLED' || s === 'CANCELADO') return 'cancelled'
  if (s === 'CREATED' || s === 'CRIADO') return 'created'
  return ''
}

function OrderStatusBadge({ order }: { order: OrderListItem }) {
  const status = normalizeStatus(order.status)
  if (status === 'paid') {
    return (
      <Badge variant="success" className={BADGE_CLASS}>
        Pago
      </Badge>
    )
  }
  if (status === 'cancelled') {
    return (
      <Badge variant="destructive" className={BADGE_CLASS}>
        Cancelado
      </Badge>
    )
  }
  if (status === 'created' && order.requiresManualApproval) {
    return (
      <Tooltip content="Criado (aguardando aprovação)">
        <Badge variant="warning" className={BADGE_CLASS}>
          Pendente
        </Badge>
      </Tooltip>
    )
  }
  return (
    <Badge variant="default" className={BADGE_CLASS}>
      Criado
    </Badge>
  )
}

interface OrderTableRowProps {
  order: OrderListItem
  onApprove: (id: string | number) => void
  isApproving: boolean
}

export function OrderTableRow({
  order,
  onApprove,
  isApproving,
}: OrderTableRowProps) {
  return (
    <tr className={tableStyles.row}>
      <td className={tableStyles.cell}>
        <div className={tableStyles.cellFlex}>
          <span className="font-mono text-xs leading-none text-slate-600 whitespace-nowrap" title={String(order.id)}>
            {order.id}
          </span>
        </div>
      </td>
      <td className={tableStyles.cell}>
        <div className={tableStyles.cellClient}>
          <span
            className="block max-w-full truncate text-sm text-slate-800"
            title={order.customerName}
          >
            {order.customerName}
          </span>
        </div>
      </td>
      <td className={tableStyles.cellRight}>
        <div className={tableStyles.cellFlexEnd}>
          <span className="whitespace-nowrap text-sm font-medium text-slate-900">
            {formatCurrency(order.totalAmount)}
          </span>
        </div>
      </td>
      <td className={tableStyles.cell}>
        <div className={tableStyles.cellFlex}>
          <span className="whitespace-nowrap text-sm text-slate-600">
            {formatOrderDate(order.orderDate)}
          </span>
        </div>
      </td>
      <td className={tableStyles.cell}>
        <div className={tableStyles.cellFlexCenter}>
          <span className="inline-flex items-center justify-center">
            <OrderStatusBadge order={order} />
          </span>
        </div>
      </td>
      <td className={tableStyles.cell}>
        <div className={tableStyles.cellFlexCenter}>
          {order.requiresManualApproval ? (
            <Button
              variant="success"
              size="sm"
              disabled={isApproving || normalizeStatus(order.status) === 'paid'}
              onClick={() => onApprove(order.id)}
              className="shrink-0 leading-none"
            >
              Aprovar
            </Button>
          ) : (
            <span className="inline-block h-8 w-24 shrink-0" aria-hidden />
          )}
        </div>
      </td>
    </tr>
  )
}
