/**
 * Utilitários de hash e criptografia usando a Web Crypto API (nativa do browser).
 * Utiliza VITE_ENCRYPTION_KEY do ambiente para operações que envolvem chave.
 *
 * IMPORTANTE — Segurança em produção:
 * Para chaves sensíveis (ex.: AWS_ACCESS_KEY_ID, chaves de criptografia),
 * em cenário real utilizaríamos AWS Secrets Manager (ou equivalente) injetado
 * no pipeline de build ou em runtime, e nunca as salvaríamos no repositório Git
 * nem em variáveis .env commitadas. As variáveis VITE_* são embutidas no bundle
 * do cliente e são visíveis a quem inspeciona o front-end; use-as apenas para
 * configuração não sensível ou para fluxos em que a chave é necessária no cliente
 * com aceitação de risco (ex.: ofuscação de dados não críticos).
 */

const ENCRYPTION_KEY_ENV = import.meta.env.VITE_ENCRYPTION_KEY as string | undefined

const PBKDF2_ITERATIONS = 100_000
const SALT = new Uint8Array(16) // Em produção, use um salt único por operação e armazene-o.

/**
 * Gera hash SHA-256 de uma string e retorna em hexadecimal.
 */
export async function hash(value: string): Promise<string> {
  const encoder = new TextEncoder()
  const data = encoder.encode(value)
  const digest = await crypto.subtle.digest('SHA-256', data)
  return Array.from(new Uint8Array(digest))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('')
}

/**
 * Deriva uma CryptoKey a partir da VITE_ENCRYPTION_KEY (PBKDF2).
 * Retorna null se a chave de ambiente não estiver definida.
 */
async function deriveKey(): Promise<CryptoKey | null> {
  if (!ENCRYPTION_KEY_ENV || ENCRYPTION_KEY_ENV.trim() === '') return null
  const encoder = new TextEncoder()
  const keyMaterial = await crypto.subtle.importKey(
    'raw',
    encoder.encode(ENCRYPTION_KEY_ENV),
    'PBKDF2',
    false,
    ['deriveBits', 'deriveKey'],
  )
  return crypto.subtle.deriveKey(
    {
      name: 'PBKDF2',
      salt: SALT,
      iterations: PBKDF2_ITERATIONS,
      hash: 'SHA-256',
    },
    keyMaterial,
    { name: 'AES-GCM', length: 256 },
    false,
    ['encrypt', 'decrypt'],
  )
}

const IV_LENGTH = 12

/**
 * Criptografa um texto usando AES-GCM e a chave derivada de VITE_ENCRYPTION_KEY.
 * Retorna null se a chave não estiver configurada.
 * Formato: iv (12 bytes) + ciphertext (base64).
 */
export async function encrypt(plainText: string): Promise<string | null> {
  const key = await deriveKey()
  if (!key) return null
  const iv = crypto.getRandomValues(new Uint8Array(IV_LENGTH))
  const encoder = new TextEncoder()
  const ciphertext = await crypto.subtle.encrypt(
    { name: 'AES-GCM', iv },
    key,
    encoder.encode(plainText),
  )
  const combined = new Uint8Array(iv.length + ciphertext.byteLength)
  combined.set(iv)
  combined.set(new Uint8Array(ciphertext), iv.length)
  return btoa(String.fromCharCode(...combined))
}

/**
 * Descriptografa um payload produzido por encrypt().
 * Retorna null se a chave não estiver configurada ou se o payload for inválido.
 */
export async function decrypt(encoded: string): Promise<string | null> {
  const key = await deriveKey()
  if (!key) return null
  try {
    const combined = Uint8Array.from(atob(encoded), (c) => c.charCodeAt(0))
    const iv = combined.slice(0, IV_LENGTH)
    const ciphertext = combined.slice(IV_LENGTH)
    const decrypted = await crypto.subtle.decrypt(
      { name: 'AES-GCM', iv },
      key,
      ciphertext,
    )
    return new TextDecoder().decode(decrypted)
  } catch {
    return null
  }
}
