/** Classes centralizadas para a tabela de pedidos (table-fixed layout). */
/** Altura fixa 64px (h-16) em células e wrappers para alinhamento vertical consistente. */
export const tableStyles = {
  cell: 'align-middle h-16 min-h-[4rem] px-4 py-3',
  cellRight: 'align-middle h-16 min-h-[4rem] px-4 py-3 text-right',
  cellFlex: 'flex h-16 min-h-[4rem] items-center',
  cellFlexEnd: 'flex h-16 min-h-[4rem] items-center justify-end',
  cellFlexCenter: 'flex h-16 min-h-[4rem] w-full items-center justify-center',
  cellClient: 'flex h-16 min-h-[4rem] min-w-0 max-w-full items-center overflow-hidden',
  thBase: 'px-4 py-3 text-sm font-semibold text-slate-600',
  thId: 'w-[96px] px-4 py-3 text-left text-sm font-semibold text-slate-600',
  thClient: 'w-auto px-4 py-3 text-left text-sm font-semibold text-slate-600',
  thValue: 'w-[140px] px-4 py-3 text-right text-sm font-semibold text-slate-600',
  thDate: 'w-[120px]',
  thStatus: 'w-[130px] px-4 py-3 text-center text-sm font-semibold text-slate-600',
  thActions: 'w-[100px] px-4 py-3 text-center text-sm font-semibold text-slate-600',
  row: 'h-16 min-h-[4rem] border-b border-slate-200 hover:bg-slate-50/80 transition-colors',
  thead: 'sticky top-0 z-10 border-b border-slate-200 bg-slate-50/50',
  sortButton:
    'inline-flex items-center gap-1 hover:text-minerva-blue focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue focus-visible:ring-offset-1 rounded',
} as const
