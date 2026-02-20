import type {
  Order,
  OrderStatusLegacy,
  OrderListItem,
  OrdersListResponse,
} from '@/shared/models'

const DELAY_MS = 500

/** Status numérico para API (0=Criado, 1=Pago, 2=Cancelado, 3=Pendente). */
const STATUS_API = { CREATED: 0, PAID: 1, CANCELLED: 2, PENDING: 3 } as const

export const CUSTOMER_NAMES = [
  'Fazenda Rio Grande',
  'Agropecuária Vale Verde',
  'Cooperativa Sul',
  'Distribuidora Norte',
  'Atacado Centro-Oeste',
  'Frigorífico Boi Gordo',
  'Cerealista Planalto',
  'Suinocultura Integrada',
  'Avicultura Santa Maria',
  'Grãos e Insumos Ltda',
  'Agrícola Pioneira',
  'Peixaria do Litoral',
  'Hortifruti Central',
  'Laticínios Serra',
  'Exportadora Minerva',
  'Comercial Agrícola',
  'Fazenda Santa Helena',
  'Cooperativa Agropecuária',
  'Alimentos Brasil',
  'Carnes Selectas',
]

/** Gera 30 pedidos realistas distribuídos nos últimos 30 dias. */
function generateOrders(): Order[] {
  const list: Order[] = []
  const statuses: OrderStatusLegacy[] = ['CREATED', 'PAID', 'CANCELLED']
  const now = Date.now()
  const dayMs = 24 * 60 * 60 * 1000

  for (let i = 1; i <= 30; i++) {
    const daysAgo = Math.floor(Math.random() * 30)
    const createdAt = new Date(now - daysAgo * dayMs).toISOString()
    const customerIndex = Math.floor(Math.random() * CUSTOMER_NAMES.length)
    const customerName = CUSTOMER_NAMES[customerIndex]
    const customerId = `C${String(customerIndex + 1).padStart(3, '0')}`
    const totalAmount = Math.round(
      1500 + Math.random() * 10500,
    )
    const status =
      statuses[Math.floor(Math.random() * statuses.length)] as OrderStatusLegacy
    const requiresManualApproval =
      status === 'CREATED' && totalAmount > 5_000

    list.push({
      id: `ORD-${1000 + i}`,
      customerId,
      customerName,
      totalAmount,
      status,
      requiresManualApproval,
      createdAt,
    })
  }

  return list
}

const orders = generateOrders()

export function getOrders(): Order[] {
  return [...orders]
}

/** Status para filtro (numérico na API): 0=Criado, 1=Pago, 2=Cancelado, 3=Pendente. */
export type OrderStatusFilter = OrderStatusLegacy | 'PENDING'

export interface OrdersQueryParams {
  page?: number
  limit?: number
  sortBy?: 'date' | 'status'
  order?: 'asc' | 'desc'
  status?: OrderStatusFilter
  date?: string
  dateFrom?: string
  dateTo?: string
}

export interface OrdersMetaResponse {
  data: Order[]
  total: number
  page: number
  lastPage: number
}

/** Filtra, ordena e pagina o array em memória. */
export function getOrdersFiltered(params: OrdersQueryParams): OrdersMetaResponse {
  const page = Math.max(1, params.page ?? 1)
  const limit = Math.min(50, Math.max(1, params.limit ?? 10))
  const sortBy = params.sortBy ?? 'date'
  const order = params.order ?? 'desc'

  let list = orders.filter((o) => {
    if (params.status) {
      if (params.status === 'PENDING') {
        if (!o.requiresManualApproval) return false
      } else if (o.status !== params.status) return false
    }
    const created = o.createdAt ? new Date(o.createdAt).getTime() : 0
    if (params.date) {
      const d = new Date(params.date)
      const dayStart = new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
      const dayEnd = dayStart + 24 * 60 * 60 * 1000 - 1
      if (created < dayStart || created > dayEnd) return false
    }
    if (params.dateFrom) {
      const from = new Date(params.dateFrom).getTime()
      if (created < from) return false
    }
    if (params.dateTo) {
      const to = new Date(params.dateTo).getTime() + 24 * 60 * 60 * 1000 - 1
      if (created > to) return false
    }
    return true
  })

  list = [...list].sort((a, b) => {
    let cmp = 0
    if (sortBy === 'date') {
      const da = a.createdAt ? new Date(a.createdAt).getTime() : 0
      const db = b.createdAt ? new Date(b.createdAt).getTime() : 0
      cmp = da - db
    } else {
      cmp = (a.status ?? '').localeCompare(b.status ?? '')
    }
    return order === 'asc' ? cmp : -cmp
  })

  const total = list.length
  const lastPage = Math.max(1, Math.ceil(total / limit))
  const start = (page - 1) * limit
  const data = list.slice(start, start + limit)

  return { data, total, page, lastPage }
}

/** Extrai id numérico do id do mock (ex: "ORD-1001" -> 1001). */
function orderNumericId(o: Order): number {
  const n = parseInt(o.id.replace(/\D/g, ''), 10)
  return Number.isNaN(n) ? 0 : n
}

/** Converte Order (mock) para OrderListItem (contrato GET /api/Orders). */
function toOrderListItem(o: Order): OrderListItem {
  const id = orderNumericId(o)
  return {
    id,
    customerId: id,
    customerName: o.customerName,
    paymentConditionId: 1,
    paymentConditionDescription: 'À vista',
    orderDate: o.createdAt ?? new Date().toISOString(),
    totalAmount: o.totalAmount,
    status: o.status,
    requiresManualApproval: o.requiresManualApproval,
    deliveryDays: 7,
    estimatedDeliveryDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
    items: [],
  }
}

export interface OrdersListQueryParams {
  pageNumber?: number
  pageSize?: number
  status?: number
  dateFrom?: string
  dateTo?: string
}

/** Retorna listagem no contrato GET /api/Orders (items, totalCount, pageNumber, pageSize). */
export function getOrdersListResponse(
  params: OrdersListQueryParams,
): OrdersListResponse {
  const pageNumber = Math.max(1, params.pageNumber ?? 1)
  const pageSize = Math.min(50, Math.max(1, params.pageSize ?? 20))

  let list = orders.filter((o) => {
    if (params.status !== undefined && params.status !== null) {
      if (params.status === STATUS_API.PENDING) {
        if (!o.requiresManualApproval) return false
      } else if (
        params.status === STATUS_API.CREATED && o.status !== 'CREATED') return false
      else if (params.status === STATUS_API.PAID && o.status !== 'PAID') return false
      else if (params.status === STATUS_API.CANCELLED && o.status !== 'CANCELLED') return false
    }
    const created = o.createdAt ? new Date(o.createdAt).getTime() : 0
    if (params.dateFrom) {
      const from = parseDateParam(params.dateFrom)
      if (!isNaN(from) && created < from) return false
    }
    if (params.dateTo) {
      const to = parseDateParam(params.dateTo)
      if (!isNaN(to) && created > to + 24 * 60 * 60 * 1000 - 1) return false
    }
    return true
  })

  list = [...list].sort((a, b) => {
    const da = a.createdAt ? new Date(a.createdAt).getTime() : 0
    const db = b.createdAt ? new Date(b.createdAt).getTime() : 0
    return db - da
  })

  const totalCount = list.length
  const start = (pageNumber - 1) * pageSize
  const page = list.slice(start, start + pageSize)
  const items: OrderListItem[] = page.map((o) => toOrderListItem(o))

  return { items, totalCount, pageNumber, pageSize }
}

function parseDateParam(ddMmYyyy: string): number {
  const parts = ddMmYyyy.split('/')
  if (parts.length !== 3) return NaN
  const [d, m, y] = parts.map((p) => parseInt(p, 10))
  if (parts.some((_, i) => [d, m, y][i] === undefined || isNaN([d, m, y][i] as number))) return NaN
  return new Date(y!, m! - 1, d!).getTime()
}

/** Aprova um pedido in-place; retorna o pedido atualizado ou null. Aceita id string (ORD-1001) ou number (1001). */
export function approveOrderById(orderId: string | number): Order | null {
  const idStr =
    typeof orderId === 'number' ? `ORD-${orderId}` : orderId
  const index = orders.findIndex((o) => o.id === idStr)
  if (index === -1) return null
  orders[index] = {
    ...orders[index],
    status: 'PAID',
    requiresManualApproval: false,
  }
  return orders[index]
}

export const MOCK_DELAY_MS = DELAY_MS

export function delay(ms: number = DELAY_MS): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms))
}
