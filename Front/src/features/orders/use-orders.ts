import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { approveOrder, listOrders, type ListOrdersParams } from './orders-api'

const ORDERS_QUERY_KEY = ['orders']

/** Chave estável por valor (evita refetch quando o objeto params muda de referência). */
function ordersQueryKey(params: ListOrdersParams): unknown[] {
  return [
    ORDERS_QUERY_KEY[0],
    params.pageNumber ?? 1,
    params.pageSize ?? 20,
    params.status ?? '',
    params.dateFrom ?? '',
    params.dateTo ?? '',
  ]
}

export function useOrders(params: ListOrdersParams = {}) {
  return useQuery({
    queryKey: ordersQueryKey(params),
    queryFn: () => listOrders(params),
  })
}

export function useApproveOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (orderId: string | number) => approveOrder(orderId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ORDERS_QUERY_KEY })
    },
  })
}
