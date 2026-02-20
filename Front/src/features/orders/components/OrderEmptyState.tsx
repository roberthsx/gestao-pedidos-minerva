import { Package } from 'lucide-react'

export function OrderEmptyState() {
  return (
    <tr>
      <td colSpan={6} className="h-64 align-middle px-4">
        <div className="flex flex-col items-center justify-center gap-3 text-center">
          <div className="rounded-full bg-slate-100 p-4">
            <Package className="h-10 w-10 text-slate-400" aria-hidden />
          </div>
          <p className="text-sm font-medium text-slate-700">
            Nenhum pedido encontrado
          </p>
          <p className="text-xs text-slate-500 max-w-xs">
            Tente ajustar os filtros de status ou data para ver outros resultados.
          </p>
        </div>
      </td>
    </tr>
  )
}
