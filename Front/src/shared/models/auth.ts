import { z } from 'zod'

/** Perfis de acesso (Roles) para controle de rotas e funcionalidades. */
export type Role = 'MANAGER' | 'ANALYST' | 'ADMIN'

export const ROLES = {
  MANAGER: 'MANAGER',
  ANALYST: 'ANALYST',
  ADMIN: 'ADMIN',
} as const satisfies Record<string, Role>

/** Rótulos para exibição (Gestor / Analista / Admin). */
export const ROLE_LABELS: Record<Role, string> = {
  MANAGER: 'Gestor',
  ANALYST: 'Analista',
  ADMIN: 'Admin',
}

/** Roles que podem criar novo pedido (RBAC). */
export const ROLES_CAN_CREATE_ORDER: Role[] = [ROLES.MANAGER, ROLES.ADMIN]

export function canCreateOrder(role: Role | undefined): boolean {
  return role != null && ROLES_CAN_CREATE_ORDER.includes(role)
}

/** Contrato do corpo da requisição de login. Back-end aceita "senha" (compatibilidade). */
export interface LoginRequest {
  registrationNumber: string
  /** Enviado como "senha" no JSON para o back-end. */
  senha: string
}

/**
 * Schema Zod da resposta de login (envelope completo, para validação opcional).
 * O interceptor já desempacota; o app recebe apenas data.
 */
export const loginResponseEnvelopeSchema = z.object({
  success: z.boolean(),
  data: z.object({
    accessToken: z.string(),
    expiresIn: z.number(),
    user: z.object({
      name: z.string(),
      role: z.string(),
    }),
  }),
})

/** Dados de login após unwrap do envelope (o que o app efetivamente recebe). */
export const loginResponseDataSchema = z.object({
  accessToken: z.string(),
  expiresIn: z.number(),
  user: z.object({
    name: z.string(),
    role: z.string(),
  }),
})

export type LoginResponseData = z.infer<typeof loginResponseDataSchema>

/** Usuário retornado pela API (contrato em inglês: name, role). */
export interface User {
  name: string
  role: Role
}

/** Contrato da resposta da API de login (após unwrap do envelope). */
export interface LoginResponse {
  accessToken: string
  expiresIn: number
  user: User
}
