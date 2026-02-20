import { http, HttpResponse } from 'msw'
import {
  getOrdersListResponse,
  approveOrderById,
  delay,
  MOCK_DELAY_MS,
  CUSTOMER_NAMES,
} from './data'
import type { OrderListItem } from '@/shared/models'

const MOCK_TOKEN = 'jwt-mock-token'

/** Base URL para os handlers (VITE_API_URL no Vitest e no dev; fallback /api). */
const API_BASE =
  (typeof import.meta !== 'undefined' && import.meta.env?.VITE_API_URL) || '/api'

const MOCK_CUSTOMERS = CUSTOMER_NAMES.map((name, i) => ({
  id: i + 1,
  name,
}))

const MOCK_PAYMENT_CONDITIONS = [
  { id: 1, description: 'À vista', numberOfInstallments: 1 },
  { id: 2, description: '30 dias', numberOfInstallments: 1 },
  { id: 3, description: '60 dias', numberOfInstallments: 1 },
  { id: 4, description: '90 dias', numberOfInstallments: 1 },
]

/** Resolve role para login: usuário contendo 'admin' ou 'manager' → MANAGER, senão ANALYST. */
function resolveLoginRole(username: string): 'MANAGER' | 'ANALYST' {
  const u = (username ?? '').toLowerCase()
  if (u.includes('admin') || u.includes('manager')) return 'MANAGER'
  return 'ANALYST'
}

/** Path do login (PascalCase alinhado ao back-end .NET). */
export const handlers = [
  http.post(`${API_BASE}/Auth/login`, async ({ request }) => {
    await delay(MOCK_DELAY_MS)
    let body: { registrationNumber?: string; senha?: string } = {}
    try {
      body = (await request.json()) as { registrationNumber?: string; senha?: string }
    } catch {
      return HttpResponse.json(
        { message: 'Body inválido' },
        { status: 400 },
      )
    }
    const registrationNumber = body.registrationNumber ?? ''
    const role = resolveLoginRole(registrationNumber)
    return HttpResponse.json({
      accessToken: MOCK_TOKEN,
      expiresIn: 3600,
      user: { name: registrationNumber || 'Usuário', role },
    })
  }),

  http.get(`${API_BASE}/orders`, async ({ request }) => {
    await delay(MOCK_DELAY_MS)
    const auth = request.headers.get('Authorization')
    if (!auth || !auth.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Não autorizado' },
        { status: 401 },
      )
    }
    const url = new URL(request.url)
    const pageNumber = url.searchParams.get('pageNumber')
    const pageSize = url.searchParams.get('pageSize')
    const status = url.searchParams.get('status')
    const dateFrom = url.searchParams.get('dateFrom')
    const dateTo = url.searchParams.get('dateTo')

    const result = getOrdersListResponse({
      pageNumber: pageNumber ? parseInt(pageNumber, 10) : undefined,
      pageSize: pageSize ? parseInt(pageSize, 10) : undefined,
      status: status != null && status !== '' ? parseInt(status, 10) : undefined,
      dateFrom: dateFrom ?? undefined,
      dateTo: dateTo ?? undefined,
    })
    return HttpResponse.json(result)
  }),

  http.put(`${API_BASE}/orders/:id/approve`, async ({ request, params }) => {
    await delay(MOCK_DELAY_MS)
    const auth = request.headers.get('Authorization')
    if (!auth || !auth.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Não autorizado' },
        { status: 401 },
      )
    }
    const idParam = params.id as string
    const id = /^\d+$/.test(idParam) ? parseInt(idParam, 10) : idParam
    const updated = approveOrderById(id)
    if (!updated) {
      return HttpResponse.json(
        { message: 'Pedido não encontrado' },
        { status: 404 },
      )
    }
    const listItem: OrderListItem = {
      id: parseInt(updated.id.replace(/\D/g, ''), 10) || 0,
      customerId: 0,
      customerName: updated.customerName,
      paymentConditionId: 1,
      paymentConditionDescription: 'À vista',
      orderDate: updated.createdAt ?? new Date().toISOString(),
      totalAmount: updated.totalAmount,
      status: updated.status,
      requiresManualApproval: updated.requiresManualApproval,
      deliveryDays: 7,
      estimatedDeliveryDate: new Date().toISOString(),
      items: [],
    }
    return HttpResponse.json(listItem)
  }),

  http.get(`${API_BASE}/customers`, async ({ request }) => {
    await delay(MOCK_DELAY_MS)
    const auth = request.headers.get('Authorization')
    if (!auth || !auth.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Não autorizado' },
        { status: 401 },
      )
    }
    return HttpResponse.json(MOCK_CUSTOMERS)
  }),

  http.get(`${API_BASE}/payment-conditions`, async ({ request }) => {
    await delay(MOCK_DELAY_MS)
    const auth = request.headers.get('Authorization')
    if (!auth || !auth.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Não autorizado' },
        { status: 401 },
      )
    }
    return HttpResponse.json(MOCK_PAYMENT_CONDITIONS)
  }),

  http.post(`${API_BASE}/orders`, async ({ request }) => {
    await delay(MOCK_DELAY_MS)
    const auth = request.headers.get('Authorization')
    if (!auth || !auth.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'Não autorizado' },
        { status: 401 },
      )
    }
    let body: {
      customerId?: number
      paymentConditionId?: number
      orderDate?: string
      items?: Array< { productName?: string; quantity?: number; unitPrice?: number } >
    } = {}
    try {
      body = (await request.json()) as typeof body
    } catch {
      return HttpResponse.json(
        { message: 'Body inválido' },
        { status: 400 },
      )
    }
    const customerId = body.customerId ?? 0
    const paymentConditionId = body.paymentConditionId ?? 0
    const orderDate = body.orderDate ?? new Date().toISOString()
    const items = body.items ?? []
    const customer = MOCK_CUSTOMERS.find((c) => c.id === customerId)
    const paymentCondition = MOCK_PAYMENT_CONDITIONS.find(
      (p) => p.id === paymentConditionId,
    )
    const orderItems = items.map((i) => {
      const q = Number(i.quantity) || 0
      const up = Number(i.unitPrice) || 0
      return {
        productName: String(i.productName ?? ''),
        quantity: q,
        unitPrice: up,
        totalPrice: q * up,
      }
    })
    const totalAmount = orderItems.reduce((acc, i) => acc + i.totalPrice, 0)
    const deliveryDays = paymentConditionId === 1 ? 3 : paymentConditionId === 2 ? 7 : 14
    const estimatedDate = new Date(orderDate)
    estimatedDate.setDate(estimatedDate.getDate() + deliveryDays)
    return HttpResponse.json(
      {
        id: Math.floor(Math.random() * 100000) + 1000,
        customerId,
        customerName: customer?.name ?? 'Cliente',
        paymentConditionId,
        paymentConditionDescription: paymentCondition?.description ?? '',
        orderDate,
        totalAmount,
        status: totalAmount > 5000 ? 'CREATED' : 'PAID',
        requiresManualApproval: totalAmount > 5000,
        deliveryDays,
        estimatedDeliveryDate: estimatedDate.toISOString(),
        items: orderItems,
      },
      { status: 201 },
    )
  }),
]
