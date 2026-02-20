import { LoginForm } from './LoginForm'

/** Tela de login: formulário isolado para facilitar testes e reuso. */
export function LoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 px-4">
      <div className="w-full max-w-md rounded-2xl bg-white/95 p-8 shadow-2xl">
        <h1 className="mb-2 text-center text-2xl font-semibold text-slate-900">
          Minerva Foods
        </h1>
        <p className="mb-8 text-center text-sm text-slate-500">
          Sistema de Gestão de Pedidos
        </p>
        <LoginForm />
      </div>
    </div>
  )
}
