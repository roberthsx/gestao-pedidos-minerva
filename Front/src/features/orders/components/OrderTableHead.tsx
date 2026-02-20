import { ChevronUp, ChevronDown } from 'lucide-react'
import { cn } from '@/shared/utils'
import { tableStyles } from './order-table-styles'

interface SortHeaderProps {
  label: string
  active: boolean
  order: 'asc' | 'desc'
  onSort: () => void
  className?: string
  center?: boolean
}

function SortHeader({
  label,
  active,
  order,
  onSort,
  className,
  center,
}: SortHeaderProps) {
  return (
    <th className={cn(tableStyles.thBase, center ? 'text-center' : 'text-left', className)}>
      <button
        type="button"
        onClick={onSort}
        className={tableStyles.sortButton}
      >
        {label}
        {active &&
          (order === 'asc' ? (
            <ChevronUp className="h-4 w-4" aria-hidden />
          ) : (
            <ChevronDown className="h-4 w-4" aria-hidden />
          ))}
      </button>
    </th>
  )
}

interface OrderTableHeadProps {
  sortBy: 'date' | 'status'
  order: 'asc' | 'desc'
  onSort: (field: 'date' | 'status') => void
}

export function OrderTableHead({ sortBy, order, onSort }: OrderTableHeadProps) {
  return (
    <thead className={tableStyles.thead}>
      <tr>
        <th className={tableStyles.thId}>ID</th>
        <th className={tableStyles.thClient}>Nome do Cliente</th>
        <th className={tableStyles.thValue}>Valor Total</th>
        <SortHeader
          label="Data do Pedido"
          active={sortBy === 'date'}
          order={order}
          onSort={() => onSort('date')}
          className={tableStyles.thDate}
        />
        <SortHeader
          label="Status"
          active={sortBy === 'status'}
          order={order}
          onSort={() => onSort('status')}
          className={tableStyles.thStatus}
          center
        />
        <th className={tableStyles.thActions}>Ações</th>
      </tr>
    </thead>
  )
}
