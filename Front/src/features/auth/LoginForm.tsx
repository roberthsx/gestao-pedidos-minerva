import { useForm } from 'react-hook-form'
import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Button, Input } from '@/shared/components'
import { useAuth } from '@/core/auth/use-auth'

type LoginFormValues = {
  matricula: string
  senha: string
}

export function LoginForm() {
  const navigate = useNavigate()
  const location = useLocation()
  const [submitError, setSubmitError] = useState<string | null>(null)
  const { login } = useAuth()

  const {
    register,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<LoginFormValues>({
    defaultValues: { matricula: '', senha: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setSubmitError(null)
    const matricula = values.matricula.trim()
    const senha = values.senha.trim()
    if (!matricula || !senha) {
      setSubmitError('Matrícula e senha são obrigatórios.')
      return
    }
    try {
      const { user } = await login(matricula, senha)
      const fromPath =
        (location.state as { from?: { pathname: string } } | null)?.from
          ?.pathname
      const redirectTo =
        fromPath ?? (user.role === 'MANAGER' || user.role === 'ADMIN' ? '/dashboard' : '/orders')
      navigate(redirectTo, { replace: true })
    } catch (err) {
      if (err && typeof err === 'object' && (err as { response?: { status?: number } }).response?.status === 401) {
        setSubmitError('Matrícula ou senha inválidos.')
      } else {
        setSubmitError(null)
      }
    }
  })

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      <div className="space-y-1">
        <label className="block text-sm font-medium text-slate-700">
          Matrícula
        </label>
        <Input
          type="text"
          autoComplete="username"
          placeholder="Digite sua matrícula"
          {...register('matricula')}
        />
      </div>
      <div className="space-y-1">
        <label className="block text-sm font-medium text-slate-700">
          Senha
        </label>
        <Input
          type="password"
          autoComplete="current-password"
          placeholder="Digite sua senha"
          {...register('senha')}
        />
      </div>
      {submitError && (
        <p className="text-xs text-red-600" role="alert">
          {submitError}
        </p>
      )}
      <Button type="submit" className="w-full" disabled={isSubmitting}>
        {isSubmitting ? 'Entrando...' : 'Entrar'}
      </Button>
    </form>
  )
}
