import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/shared/components'

const PAGE_SIZES = [5, 10, 20] as const

const selectClass =
  'h-9 rounded-md border border-slate-300 bg-white px-2 py-1 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue'

interface OrderPaginationProps {
  page: number
  lastPage: number
  total: number
  limit: number
  onPageChange: (page: number) => void
  onPageSizeChange: (value: string) => void
}

export function OrderPagination({
  page,
  lastPage,
  total,
  limit,
  onPageChange,
  onPageSizeChange,
}: OrderPaginationProps) {
  return (
    <footer className="flex flex-wrap items-center justify-between gap-4 border-t border-slate-200 px-4 py-3 bg-slate-50/80 rounded-b-xl">
      <div className="flex items-center gap-2 text-sm text-slate-600">
        <span>Itens por página</span>
        <select
          value={limit}
          onChange={(e) => onPageSizeChange(e.target.value)}
          className={selectClass}
        >
          {PAGE_SIZES.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </div>
      <div className="flex items-center gap-2">
        <span className="text-sm text-slate-600">
          Página {page} de {lastPage} ({total}{' '}
          {total === 1 ? 'pedido' : 'pedidos'})
        </span>
        <Button
          variant="secondary"
          size="sm"
          disabled={page <= 1}
          onClick={() => onPageChange(Math.max(1, page - 1))}
          aria-label="Página anterior"
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button
          variant="secondary"
          size="sm"
          disabled={page >= lastPage}
          onClick={() => onPageChange(Math.min(lastPage, page + 1))}
          aria-label="Próxima página"
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </footer>
  )
}
