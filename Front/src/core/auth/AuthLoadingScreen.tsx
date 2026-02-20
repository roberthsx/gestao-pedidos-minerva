/**
 * Exibido enquanto o AuthContext valida o token do localStorage (evita flash para /login no F5).
 */
export function AuthLoadingScreen() {
  return (
    <div
      className="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50"
      role="status"
      aria-label="Carregando sessão"
    >
      <div
        className="h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-minerva-blue"
        aria-hidden
      />
      <p className="text-sm text-slate-500">Carregando...</p>
    </div>
  )
}
