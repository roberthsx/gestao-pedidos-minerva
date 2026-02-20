/**
 * MSW server para ambiente Node (testes Vitest).
 * Intercepta todas as chamadas HTTP (axios usa o módulo http do Node).
 */
import { setupServer } from 'msw/node'
import { handlers } from './handlers'

export const server = setupServer(...handlers)
