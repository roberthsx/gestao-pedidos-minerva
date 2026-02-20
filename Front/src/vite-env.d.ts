/// <reference types="vite/client" />

/**
 * Variáveis de ambiente expostas ao cliente (prefixo VITE_).
 * Definidas em .env.development, .env.production ou .env.local.
 */
interface ImportMetaEnv {
  /** URL base da API (ex.: https://api.minervafoods.com/v1). */
  readonly VITE_API_URL: string
  /** 'true' para ativar Mock Service Worker em desenvolvimento. */
  readonly VITE_ENABLE_MSW: string
  /** Região AWS (placeholder para infraestrutura). */
  readonly VITE_AWS_REGION: string
  /** Access Key AWS (placeholder — em produção use Secrets Manager). */
  readonly VITE_AWS_ACCESS_KEY_ID: string
  /** Chave para operações de criptografia (placeholder). */
  readonly VITE_ENCRYPTION_KEY: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
