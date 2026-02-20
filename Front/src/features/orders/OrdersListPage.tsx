import { useState, useCallback, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'react-toastify'
import { Card, Tooltip } from '@/shared/components'
import { useAuth } from '@/core/auth/use-auth'
import { canCreateOrder } from '@/shared/models'
import { useApproveOrder, useOrders } from './use-orders'
import { MANUAL_APPROVAL_THRESHOLD } from './order-rules'
import {
  formatDateForApi,
  orderStatusToApiValue,
  type ListOrdersParams,
} from './orders-api'
import type { OrderStatus } from '@/shared/models'

const NEW_ORDER_TOOLTIP = 'Apenas Gestores e Admins podem criar pedidos.'
import { OrderFilterBar } from './components/OrderFilterBar'
import { OrderTableHead } from './components/OrderTableHead'
import { OrderTableRow } from './components/OrderTableRow'
import { OrderPagination } from './components/OrderPagination'
import { OrderTableSkeleton } from './components/OrderTableSkeleton'
import { OrderEmptyState } from './components/OrderEmptyState'

export function OrdersListPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const allowedToCreate = canCreateOrder(user?.role)

  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [sortBy, setSortBy] = useState<'date' | 'status'>('date')
  const [order, setOrder] = useState<'asc' | 'desc'>('desc')
  const [statusFilter, setStatusFilter] = useState<'' | OrderStatus>('')
  const [dateFilter, setDateFilter] = useState('')

  const handleNewOrder = useCallback(() => {
    if (!allowedToCreate) return
    navigate('/orders/new')
  }, [allowedToCreate, navigate])

  const params: ListOrdersParams = {
    pageNumber,
    pageSize,
    ...(statusFilter !== '' ? { status: orderStatusToApiValue(statusFilter) } : {}),
    ...(dateFilter
      ? {
          dateFrom: formatDateForApi(dateFilter),
          dateTo: formatDateForApi(dateFilter),
        }
      : {}),
  }

  const { data, isLoading, isError, error } = useOrders(params)
  const { mutateAsync: approve, isPending: isApproving } = useApproveOrder()

  useEffect(() => {
    if (!isError || !error) return
    const status = (error as { response?: { status?: number } })?.response?.status
    if (status === 400) {
      toast.warning(
        'O filtro de status selecionado é inválido. Tente outro filtro.',
        { autoClose: 5000 },
      )
      setStatusFilter('')
    }
  }, [isError, error])

  const orders = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const lastPage = Math.max(
    1,
    Math.ceil(totalCount / (data?.pageSize ?? pageSize)),
  )

  const handleSort = useCallback((field: 'date' | 'status') => {
    setSortBy(field)
    setOrder((prev) => (prev === 'asc' ? 'desc' : 'asc'))
    setPageNumber(1)
  }, [])
  const handleStatusChange = useCallback((v: '' | OrderStatus) => {
    setStatusFilter(v)
    setPageNumber(1)
  }, [])
  const handleDateChange = useCallback((v: string) => {
    setDateFilter(v)
    setPageNumber(1)
  }, [])
  const handlePageSizeChange = useCallback((v: string) => {
    setPageSize(Number(v))
    setPageNumber(1)
  }, [])
  const handlePageChange = useCallback((p: number) => setPageNumber(p), [])

  if (isError) return <p className="text-sm text-red-600">Não foi possível carregar a lista de pedidos.</p>

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Pedidos</h1>
          <p className="text-sm text-slate-500">
            Listagem com aprovação manual para pedidos acima de R${' '}
            {MANUAL_APPROVAL_THRESHOLD.toLocaleString('pt-BR')},00.
          </p>
        </div>
        {allowedToCreate ? (
          <button
            type="button"
            onClick={handleNewOrder}
            className="inline-flex items-center justify-center gap-2 rounded-md bg-minerva-blue px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-[#005ba8] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue focus-visible:ring-offset-2"
          >
            Novo pedido
          </button>
        ) : (
          <Tooltip content={NEW_ORDER_TOOLTIP} side="bottom">
            <span className="inline-block">
              <button
                type="button"
                disabled
                onClick={handleNewOrder}
                aria-disabled="true"
                aria-label={NEW_ORDER_TOOLTIP}
                className="inline-flex items-center justify-center gap-2 rounded-md bg-minerva-blue px-4 py-2 text-sm font-medium text-white opacity-50 cursor-not-allowed transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue focus-visible:ring-offset-2"
              >
                Novo pedido
              </button>
            </span>
          </Tooltip>
        )}
      </header>

      <OrderFilterBar
        statusFilter={statusFilter}
        dateFilter={dateFilter}
        onStatusChange={handleStatusChange}
        onDateChange={handleDateChange}
      />

      <Card className="p-0 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="table-fixed w-full border-collapse text-sm">
            <OrderTableHead sortBy={sortBy} order={order} onSort={handleSort} />
            <tbody className="bg-white">
              {isLoading && <OrderTableSkeleton rows={pageSize} />}
              {!isLoading &&
                orders.map((orderItem) => (
                  <OrderTableRow
                    key={orderItem.id}
                    order={orderItem}
                    onApprove={approve}
                    isApproving={isApproving}
                  />
                ))}
              {!isLoading && orders.length === 0 && <OrderEmptyState />}
            </tbody>
          </table>
        </div>

        <OrderPagination
          page={pageNumber}
          lastPage={lastPage}
          total={totalCount}
          limit={pageSize}
          onPageChange={handlePageChange}
          onPageSizeChange={handlePageSizeChange}
        />
      </Card>
    </div>
  )
}
