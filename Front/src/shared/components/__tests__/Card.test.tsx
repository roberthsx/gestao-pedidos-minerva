import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Card } from '../Card'

describe('Card', () => {
  it('renderiza children', () => {
    render(<Card>Título do card</Card>)
    expect(screen.getByText('Título do card')).toBeInTheDocument()
  })

  it('aplica className adicional', () => {
    const { container } = render(
      <Card className="p-0 overflow-hidden">Conteúdo</Card>,
    )
    const card = container.firstChild as HTMLElement
    expect(card).toHaveClass('rounded-xl', 'p-0', 'overflow-hidden')
  })
})
