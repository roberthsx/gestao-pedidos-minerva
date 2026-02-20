import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
  Cell,
} from 'recharts'
import {
  DollarSign,
  Clock,
  Package,
  TrendingUp,
  ListOrdered,
  Award,
} from 'lucide-react'
import { Card, Button } from '@/shared/components'
import { formatCurrency } from '@/shared/utils'
import { useOrders } from '@/features/orders/use-orders'
import type { OrderListItem } from '@/shared/models'

/** Agrega totais e métricas a partir da lista de pedidos. */
function useDashboardMetrics(orders: OrderListItem[] | undefined) {
  return useMemo(() => {
    if (!orders?.length) {
      return {
        totalSales: 0,
        pendingApprovalCount: 0,
        avgItemsPerOrder: 0,
        avgDeliveryDays: null as number | null,
        statusCounts: { Criado: 0, Pago: 0, Cancelado: 0 },
        topClients: [] as { name: string; totalAmount: number }[],
      }
    }

    const totalSales = orders.reduce((sum, o) => sum + o.totalAmount, 0)
    const pendingApprovalCount = orders.filter(
      (o) => o.requiresManualApproval === true,
    ).length
    const createdCount = orders.filter((o) => (o.status ?? '').toUpperCase() === 'CREATED').length
    const paidCount = orders.filter((o) => (o.status ?? '').toUpperCase() === 'PAID').length
    const cancelledCount = orders.filter((o) => (o.status ?? '').toUpperCase() === 'CANCELLED').length

    // Média de itens por pedido: simulação (OrderItems não vem no mock; 1 item a cada ~R$1.5k)
    const simulatedItemsSum = orders.reduce(
      (sum, o) => sum + Math.max(1, Math.round(o.totalAmount / 1500)),
      0,
    )
    const avgItemsPerOrder =
      orders.length > 0 ? simulatedItemsSum / orders.length : 0

    // Prazo médio de entrega: N/D no mock (DeliveryTerms é assíncrono/worker)
    const avgDeliveryDays: number | null = null

    // Top 5 clientes VIP por TotalAmount (agregação por customerName)
    const byCustomer = orders.reduce<Record<string, number>>((acc, o) => {
      acc[o.customerName] = (acc[o.customerName] ?? 0) + o.totalAmount
      return acc
    }, {})
    const topClients = Object.entries(byCustomer)
      .map(([name, totalAmount]) => ({ name, totalAmount }))
      .sort((a, b) => b.totalAmount - a.totalAmount)
      .slice(0, 5)

    return {
      totalSales,
      pendingApprovalCount,
      avgItemsPerOrder,
      avgDeliveryDays,
      statusCounts: {
        Criado: createdCount,
        Pago: paidCount,
        Cancelado: cancelledCount,
      },
      topClients,
    }
  }, [orders])
}

const barData = (statusCounts: {
  Criado: number
  Pago: number
  Cancelado: number
}) => [
  { status: 'Criado', quantidade: statusCounts.Criado, fill: '#0072CE' },
  { status: 'Pago', quantidade: statusCounts.Pago, fill: '#8CC63F' },
  { status: 'Cancelado', quantidade: statusCounts.Cancelado, fill: '#64748b' },
]

export function Dashboard() {
  const navigate = useNavigate()
  const { data: ordersResponse, isLoading, isError } = useOrders({
    pageSize: 100,
  })
  const orders = ordersResponse?.items ?? []
  const metrics = useDashboardMetrics(orders)

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <p className="text-slate-500">Carregando dashboard...</p>
      </div>
    )
  }

  if (isError) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white p-6 text-center shadow-sm">
        <p className="text-slate-700">
          Não foi possível carregar os dados do dashboard.
        </p>
        <Button
          variant="primary"
          className="mt-4"
          onClick={() => window.location.reload()}
        >
          Tentar novamente
        </Button>
      </div>
    )
  }

  const chartData = barData(metrics.statusCounts)

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">Dashboard</h1>
          <p className="text-sm text-slate-500">
            Visão estratégica de pedidos e vendas
          </p>
        </div>
        <Button
          variant="success"
          onClick={() => navigate('/orders')}
          className="inline-flex items-center gap-2"
        >
          <ListOrdered className="h-4 w-4" />
          Ver todos os pedidos
        </Button>
      </div>

      {/* 4 cards em 2x2: 2 em cima, 2 em baixo — mais espaço para valor completo */}
      <div className="grid grid-cols-2 gap-4">
        <Card className="min-h-[7.5rem] overflow-hidden border-l-4 border-l-minerva-green bg-white flex flex-col justify-center">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <div className="min-w-0 flex-1">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Total em vendas (R$)
              </p>
              <p className="mt-1 text-2xl font-semibold text-slate-900">
                {formatCurrency(metrics.totalSales)}
              </p>
            </div>
            <div className="shrink-0 rounded-full bg-minerva-green/20 p-3">
              <DollarSign className="h-6 w-6 text-minerva-green" />
            </div>
          </div>
        </Card>

        <Card className="min-h-[7.5rem] overflow-hidden border-l-4 border-l-minerva-blue bg-white flex flex-col justify-center">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <div className="min-w-0 flex-1">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Pendentes de aprovação
              </p>
              <p className="mt-1 text-2xl font-semibold text-slate-900">
                {metrics.pendingApprovalCount}
              </p>
            </div>
            <div className="shrink-0 rounded-full bg-minerva-blue-light p-3">
              <Clock className="h-6 w-6 text-minerva-blue" />
            </div>
          </div>
        </Card>

        <Card className="min-h-[7.5rem] overflow-hidden border-l-4 border-l-minerva-blue bg-white flex flex-col justify-center">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <div className="min-w-0 flex-1">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Média itens/pedido
              </p>
              <p className="mt-1 text-2xl font-semibold text-slate-900">
                {metrics.avgItemsPerOrder > 0
                  ? metrics.avgItemsPerOrder.toFixed(1)
                  : '—'}
              </p>
            </div>
            <div className="shrink-0 rounded-full bg-minerva-blue-light p-3">
              <Package className="h-6 w-6 text-minerva-blue" />
            </div>
          </div>
        </Card>

        <Card className="min-h-[7.5rem] overflow-hidden border-l-4 border-l-minerva-navy bg-white flex flex-col justify-center">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <div className="min-w-0 flex-1">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Prazo médio entrega
              </p>
              <p className="mt-1 text-2xl font-semibold text-slate-900">
                {metrics.avgDeliveryDays != null
                  ? `${metrics.avgDeliveryDays} dias`
                  : 'N/D'}
              </p>
            </div>
            <div className="shrink-0 rounded-full bg-minerva-navy/10 p-3">
              <TrendingUp className="h-6 w-6 text-minerva-navy" />
            </div>
          </div>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Gráfico de barras: Criado vs Pago */}
        <Card className="overflow-hidden">
          <h2 className="mb-4 text-lg font-semibold text-slate-900">
            Pedidos por status
          </h2>
          <div className="h-[280px] w-full">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 8, right: 8, left: 8, bottom: 8 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-slate-200" />
                <XAxis dataKey="status" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip
                  contentStyle={{
                    borderRadius: '8px',
                    border: '1px solid #e2e8f0',
                  }}
                  formatter={(value: number) => [value, 'Quantidade']}
                />
                <Legend />
                <Bar dataKey="quantidade" name="Quantidade" radius={[4, 4, 0, 0]}>
                  {chartData.map((entry, index) => (
                    <Cell key={index} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Card>

        {/* Top 5 Clientes VIP */}
        <Card>
          <h2 className="mb-4 flex items-center gap-2 text-lg font-semibold text-slate-900">
            <Award className="h-5 w-5 text-minerva-green" />
            Clientes VIP (por faturamento)
          </h2>
          {metrics.topClients.length === 0 ? (
            <p className="py-8 text-center text-sm text-slate-500">
              Nenhum pedido para ranquear.
            </p>
          ) : (
            <ul className="space-y-3">
              {metrics.topClients.map((client, index) => (
                <li
                  key={client.name}
                  className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50/50 px-4 py-3 shadow-sm"
                >
                  <span className="flex items-center gap-2 font-medium text-slate-800">
                    <span className="flex h-6 w-6 items-center justify-center rounded-full bg-minerva-blue-light text-xs font-semibold text-minerva-blue">
                      {index + 1}
                    </span>
                    {client.name}
                  </span>
                  <span className="font-semibold text-minerva-navy">
                    {formatCurrency(client.totalAmount)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </div>
  )
}
