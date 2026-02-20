import { describe, it, expect, beforeEach } from 'vitest'
import { waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { server } from '@/core/api/mocks/server'
import { render, screen, userEvent } from '@/test-utils'
import { LoginForm } from '../LoginForm'

describe('LoginForm', () => {
  beforeEach(() => {
    server.resetHandlers()
  })

  it('envia registrationNumber e senha no body (contrato da API em inglês técnico)', async () => {
    const user = userEvent.setup()
    let capturedBody: { registrationNumber?: string; senha?: string } = {}
    server.use(
      http.post('http://localhost/api/Auth/login', async ({ request }) => {
        capturedBody = (await request.json()) as typeof capturedBody
        return HttpResponse.json({
          accessToken: 'token',
          expiresIn: 3600,
          user: { name: 'Test', role: 'ANALYST' },
        })
      }),
    )

    render(<LoginForm />, { initialEntries: ['/login'] })
    await user.type(screen.getByPlaceholderText(/digite sua matrícula/i), 'admin')
    await user.type(screen.getByPlaceholderText(/digite sua senha/i), 'Admin@123')
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    await waitFor(() => {
      expect(capturedBody).toEqual({
        registrationNumber: 'admin',
        senha: 'Admin@123',
      })
    })
  })

  it('exibe mensagem de erro em português quando a API retorna 401', async () => {
    const user = userEvent.setup()
    server.use(
      http.post('http://localhost/api/Auth/login', () => {
        return HttpResponse.json(
          { message: 'Unauthorized' },
          { status: 401 },
        )
      }),
    )

    render(<LoginForm />, { initialEntries: ['/login'] })
    await user.type(screen.getByPlaceholderText(/digite sua matrícula/i), 'invalid')
    await user.type(screen.getByPlaceholderText(/digite sua senha/i), 'wrong')
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    const alert = await screen.findByRole('alert', {}, { timeout: 3000 })
    expect(alert).toHaveTextContent('Matrícula ou senha inválidos.')
  })

  it('exibe validação em português quando campos vazios', async () => {
    const user = userEvent.setup()
    render(<LoginForm />, { initialEntries: ['/login'] })

    await user.click(screen.getByRole('button', { name: /entrar/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Matrícula e senha são obrigatórios.',
    )
  })
})
