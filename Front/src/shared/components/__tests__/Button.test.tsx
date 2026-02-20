import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from '../Button'

describe('Button', () => {
  it('renderiza children e permite clique', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<Button onClick={onClick}>Clique aqui</Button>)
    const btn = screen.getByRole('button', { name: /clique aqui/i })
    expect(btn).toBeInTheDocument()
    await user.click(btn)
    expect(onClick).toHaveBeenCalledTimes(1)
  })

  it('quando disabled, não chama onClick', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(
      <Button onClick={onClick} disabled>
        Desabilitado
      </Button>,
    )
    const btn = screen.getByRole('button', { name: /desabilitado/i })
    await user.click(btn)
    expect(onClick).not.toHaveBeenCalled()
  })

  it('aplica variant success e size sm', () => {
    render(
      <Button variant="success" size="sm">
        Aprovar
      </Button>,
    )
    const btn = screen.getByRole('button', { name: /aprovar/i })
    expect(btn).toHaveClass('bg-minerva-green')
    expect(btn).toHaveClass('px-3', 'py-1.5', 'text-xs')
  })
})
