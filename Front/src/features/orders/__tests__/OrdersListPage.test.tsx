import { describe, it, expect, beforeEach } from 'vitest'
import { waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { server } from '@/core/api/mocks/server'
import { render, screen, userEvent } from '@/test-utils'
import { OrdersListPage } from '../OrdersListPage'
import { AUTH_STORAGE_KEY } from '@/core/api/constants'

function setAuthenticated() {
  const payload = {
    accessToken: 'jwt-mock-token',
    expiresIn: 3600,
    user: { name: 'Test', role: 'ANALYST' as const },
    expiresAt: Date.now() + 3600 * 1000,
  }
  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(payload))
}

describe('OrdersListPage', () => {
  beforeEach(() => {
    server.resetHandlers()
    setAuthenticated()
  })

  it('ao carregar a página, exibe a lista de pedidos vinda do mock', async () => {
    render(<OrdersListPage />, { initialEntries: ['/orders'] })

    await waitFor(() => {
      expect(screen.queryByText(/não foi possível carregar/i)).not.toBeInTheDocument()
    }, { timeout: 3000 })

    const table = document.querySelector('table')
    expect(table).toBeInTheDocument()
    await waitFor(() => {
      const rows = table?.querySelectorAll('tbody tr')
      expect(rows?.length).toBeGreaterThan(0)
    }, { timeout: 3000 })
  })

  it('ao clicar em aprovar, chama o serviço (PUT /api/orders/:id/approve)', async () => {
    let approveCalled = false
    let approveId: string | number | null = null
    server.use(
      http.put('http://localhost/api/orders/:id/approve', ({ params }) => {
        approveCalled = true
        approveId = params.id as string
        return HttpResponse.json({
          id: Number(approveId),
          customerId: 0,
          customerName: 'Cliente',
          paymentConditionId: 1,
          paymentConditionDescription: 'À vista',
          orderDate: new Date().toISOString(),
          totalAmount: 6000,
          status: 'PAID',
          requiresManualApproval: false,
          deliveryDays: 7,
          estimatedDeliveryDate: new Date().toISOString(),
          items: [],
        })
      }),
    )

    render(<OrdersListPage />, { initialEntries: ['/orders'] })

    await waitFor(() => {
      expect(screen.queryByText(/não foi possível carregar/i)).not.toBeInTheDocument()
    }, { timeout: 3000 })

    const approveBtns = await screen.findAllByRole('button', { name: /aprovar/i }, { timeout: 3000 })
    const user = userEvent.setup()
    await user.click(approveBtns[0]!)

    await waitFor(() => {
      expect(approveCalled).toBe(true)
      expect(approveId).toBeTruthy()
    })
  })

  it('exibe estado de erro amigável quando a API retorna 500', async () => {
    server.use(
      http.get('http://localhost/api/orders', () => {
        return HttpResponse.json(
          { message: 'Internal Server Error' },
          { status: 500 },
        )
      }),
    )

    render(<OrdersListPage />, { initialEntries: ['/orders'] })

    const errorMessage = await screen.findByText(
      /não foi possível carregar a lista de pedidos/i,
      {},
      { timeout: 3000 },
    )
    expect(errorMessage).toBeInTheDocument()
  })
})
