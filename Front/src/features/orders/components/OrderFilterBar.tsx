import { Input } from '@/shared/components'
import { ORDER_STATUS_OPTIONS } from '@/shared/models'
import type { OrderStatus } from '@/shared/models'

const selectClass =
  'h-10 rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue focus-visible:ring-offset-2'

interface OrderFilterBarProps {
  statusFilter: '' | OrderStatus
  dateFilter: string
  onStatusChange: (value: '' | OrderStatus) => void
  onDateChange: (value: string) => void
}

export function OrderFilterBar({
  statusFilter,
  dateFilter,
  onStatusChange,
  onDateChange,
}: OrderFilterBarProps) {
  const handleStatusChange = (value: string) => {
    if (value === '') {
      onStatusChange('')
      return
    }
    if (ORDER_STATUS_OPTIONS.includes(value as OrderStatus)) {
      onStatusChange(value as OrderStatus)
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-4">
      <label className="flex items-center gap-2 text-sm text-slate-600">
        Status
        <select
          value={statusFilter}
          onChange={(e) => handleStatusChange(e.target.value)}
          className={selectClass}
          aria-label="Filtrar por status do pedido"
        >
          <option value="">Todos os status</option>
          {ORDER_STATUS_OPTIONS.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </label>
      <label className="flex items-center gap-2 text-sm text-slate-600">
        Data
        <Input
          type="date"
          value={dateFilter}
          onChange={(e) => onDateChange(e.target.value)}
          className="w-auto"
        />
      </label>
    </div>
  )
}
