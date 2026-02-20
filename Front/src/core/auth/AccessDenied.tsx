import { useNavigate } from 'react-router-dom'
import { ShieldX } from 'lucide-react'
import { Button } from '@/shared/components'

/** Tela exibida quando o usuário não tem perfil para acessar a rota. */
export function AccessDenied() {
  const navigate = useNavigate()

  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-xl border border-amber-200 bg-amber-50/50 p-8 text-center">
      <ShieldX className="h-16 w-16 text-amber-600" />
      <h2 className="mt-4 text-xl font-semibold text-slate-900">
        Acesso negado
      </h2>
      <p className="mt-2 max-w-sm text-sm text-slate-600">
        Você não tem permissão para acessar esta página. Entre em contato com o
        administrador se achar que isso é um erro.
      </p>
      <Button
        variant="primary"
        className="mt-6"
        onClick={() => navigate('/orders', { replace: true })}
      >
        Ir para Pedidos
      </Button>
    </div>
  )
}
