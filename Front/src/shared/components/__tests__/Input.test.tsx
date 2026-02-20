import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Input } from '../Input'

describe('Input', () => {
  it('renderiza com placeholder e valor controlado', async () => {
    const user = userEvent.setup()
    render(<Input placeholder="Digite aqui" data-testid="input" />)
    const input = screen.getByTestId('input')
    expect(input).toHaveAttribute('placeholder', 'Digite aqui')
    await user.type(input, 'teste')
    expect(input).toHaveValue('teste')
  })

  it('suporta type password', () => {
    render(<Input type="password" aria-label="Senha" />)
    const input = screen.getByLabelText(/senha/i)
    expect(input).toHaveAttribute('type', 'password')
  })

  it('quando disabled, não aceita input', () => {
    render(<Input disabled aria-label="Campo desabilitado" />)
    const input = screen.getByRole('textbox', { name: /campo desabilitado/i })
    expect(input).toBeDisabled()
  })
})
