import { useEffect, useState } from 'react'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import type { Resolver } from 'react-hook-form'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Plus, Trash2 } from 'lucide-react'
import { Button, Card, Input, Modal } from '@/shared/components'
import { createOrderFormSchema, type CreateOrderFormValues } from './create-order-schema'
import { createOrder } from '@/core/services/orderService'
import { listCustomers } from '@/core/services/customerService'
import { listPaymentConditions } from '@/core/services/paymentConditionService'
import { resetCorrelationId } from '@/core/api/correlation'
import type { CreateOrderResponse } from '@/shared/models'

const selectClass =
  'h-10 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-minerva-blue focus-visible:ring-offset-2 invalid:text-slate-500'

const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

export function OrderCreatePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [successOrder, setSuccessOrder] = useState<CreateOrderResponse | null>(
    null,
  )

  useEffect(() => {
    resetCorrelationId()
  }, [])

  const { data: customers = [], isLoading: loadingCustomers } = useQuery({
    queryKey: ['customers'],
    queryFn: listCustomers,
  })

  const { data: paymentConditions = [], isLoading: loadingPaymentConditions } =
    useQuery({
      queryKey: ['payment-conditions'],
      queryFn: listPaymentConditions,
    })

  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
    setError,
    clearErrors,
  } = useForm<CreateOrderFormValues>({
    resolver: zodResolver(createOrderFormSchema) as Resolver<CreateOrderFormValues>,
    defaultValues: {
      customerId: 0,
      paymentConditionId: 0,
      orderDate: new Date().toISOString().slice(0, 16),
      items: [{ productName: '', quantity: 1, unitPrice: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'items',
  })

  const items = watch('items')
  const totalAmount =
    items?.reduce(
      (acc, item) =>
        acc + (Number(item?.quantity) || 0) * (Number(item?.unitPrice) || 0),
      0,
    ) ?? 0

  const createOrderMutation = useMutation({
    mutationFn: createOrder,
    onSuccess: (data) => {
      setSuccessOrder(data)
      queryClient.invalidateQueries({ queryKey: ['orders'] })
    },
    onError: (err: {
      response?: {
        status?: number
        data?: { message?: string; errors?: unknown }
      }
    }) => {
      const status = err.response?.status
      const data = err.response?.data
      const message =
        (typeof data?.message === 'string' && data.message) ||
        (status === 400
          ? 'Dados inválidos. Verifique os campos e tente novamente.'
          : status === 500
            ? 'Erro interno do servidor. Tente novamente mais tarde.'
            : 'Não foi possível criar o pedido. Tente novamente.')
      setError('root', { type: 'server', message })
    },
  })

  const onSubmit = handleSubmit((values) => {
    clearErrors('root')
    const orderDate =
      values.orderDate.includes('T') && values.orderDate.length >= 16
        ? new Date(values.orderDate).toISOString()
        : new Date(values.orderDate).toISOString()
    createOrderMutation.mutate(
      {
        customerId: Number(values.customerId),
        paymentConditionId: Number(values.paymentConditionId),
        orderDate,
        items: values.items.map(
          (i: CreateOrderFormValues['items'][number]) => ({
            productName: i.productName.trim(),
            quantity: Number(i.quantity),
            unitPrice: Number(i.unitPrice),
          }),
        ),
      },
      { onError: () => {} },
    )
  })

  const isSubmittingForm = isSubmitting || createOrderMutation.isPending

  return (
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">
            Cadastro de Pedido
          </h1>
          <p className="text-sm text-slate-500">
            Preencha os dados do cliente, condição de pagamento e itens.
          </p>
        </div>
        <Button
          type="button"
          variant="secondary"
          onClick={() => navigate('/orders')}
        >
          Voltar
        </Button>
      </header>

      <form onSubmit={onSubmit} className="space-y-6">
        <Card>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1">
              <label className="block text-sm font-medium text-slate-700">
                Cliente *
              </label>
              <select
                {...register('customerId')}
                className={selectClass}
                disabled={loadingCustomers}
              >
                <option value={0}>Selecione o cliente</option>
                {customers.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
              {errors.customerId && (
                <p className="text-xs text-red-600">
                  {errors.customerId.message}
                </p>
              )}
            </div>
            <div className="space-y-1">
              <label className="block text-sm font-medium text-slate-700">
                Condição de pagamento *
              </label>
              <select
                {...register('paymentConditionId')}
                className={selectClass}
                disabled={loadingPaymentConditions}
              >
                <option value={0}>Selecione a condição</option>
                {paymentConditions.map((pc) => (
                  <option key={pc.id} value={pc.id}>
                    {pc.description}
                  </option>
                ))}
              </select>
              {errors.paymentConditionId && (
                <p className="text-xs text-red-600">
                  {errors.paymentConditionId.message}
                </p>
              )}
            </div>
            <div className="space-y-1 sm:col-span-2">
              <label className="block text-sm font-medium text-slate-700">
                Data do pedido *
              </label>
              <Input
                type="datetime-local"
                {...register('orderDate')}
                className="max-w-xs"
              />
              {errors.orderDate && (
                <p className="text-xs text-red-600">
                  {errors.orderDate.message}
                </p>
              )}
            </div>
          </div>
        </Card>

        <Card>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-lg font-medium text-slate-900">Itens do pedido</h2>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => append({ productName: '', quantity: 1, unitPrice: 0 })}
            >
              <Plus className="h-4 w-4" />
              Adicionar item
            </Button>
          </div>
          {errors.items?.message && (
            <p className="mb-2 text-xs text-red-600">{errors.items.message}</p>
          )}
          <div className="space-y-4">
            {fields.map((field, index) => (
              <div
                key={field.id}
                className="flex flex-wrap items-end gap-4 rounded-lg border border-slate-200 bg-slate-50/50 p-4"
              >
                <div className="min-w-[200px] flex-1 space-y-1">
                  <label className="block text-xs font-medium text-slate-600">
                    Produto *
                  </label>
                  <Input
                    placeholder="Nome do produto"
                    {...register(`items.${index}.productName`)}
                  />
                  {errors.items?.[index]?.productName && (
                    <p className="text-xs text-red-600">
                      {errors.items[index]?.productName?.message}
                    </p>
                  )}
                </div>
                <div className="w-24 space-y-1">
                  <label className="block text-xs font-medium text-slate-600">
                    Qtd *
                  </label>
                  <Input
                    type="number"
                    min={1}
                    step={1}
                    {...register(`items.${index}.quantity`)}
                  />
                  {errors.items?.[index]?.quantity && (
                    <p className="text-xs text-red-600">
                      {errors.items[index]?.quantity?.message}
                    </p>
                  )}
                </div>
                <div className="w-32 space-y-1">
                  <label className="block text-xs font-medium text-slate-600">
                    Preço unit. *
                  </label>
                  <Input
                    type="number"
                    min={0}
                    step={0.01}
                    placeholder="0,00"
                    {...register(`items.${index}.unitPrice`)}
                  />
                  {errors.items?.[index]?.unitPrice && (
                    <p className="text-xs text-red-600">
                      {errors.items[index]?.unitPrice?.message}
                    </p>
                  )}
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => remove(index)}
                  disabled={fields.length <= 1}
                  className="text-red-600 hover:bg-red-50 hover:text-red-700"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        </Card>

        {errors.root && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {errors.root.message}
          </div>
        )}

        <Card className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-lg font-semibold text-slate-900">
            Valor total:{' '}
            <span className="text-minerva-blue">
              {currencyFormatter.format(totalAmount)}
            </span>
          </p>
          <Button type="submit" disabled={isSubmittingForm}>
            {isSubmittingForm ? 'Salvando...' : 'Criar pedido'}
          </Button>
        </Card>
      </form>

      <Modal
        open={!!successOrder}
        onClose={() => {
          setSuccessOrder(null)
          navigate('/orders')
        }}
        title="Pedido criado com sucesso"
      >
        {successOrder && (
          <div className="space-y-4">
            <p className="text-sm text-slate-600">
              <span className="font-medium">Status:</span>{' '}
              {successOrder.status === 'PAID' ? 'Pago' : 'Criado'}
            </p>
            {successOrder.requiresManualApproval && (
              <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
                Este pedido aguarda aprovação manual do gerente.
              </div>
            )}
            <p className="text-sm text-slate-600">
              <span className="font-medium">Data estimada de entrega:</span>{' '}
              {new Date(successOrder.estimatedDeliveryDate).toLocaleDateString(
                'pt-BR',
                {
                  dateStyle: 'long',
                },
              )}
            </p>
            <p className="text-sm text-slate-600">
              <span className="font-medium">Total:</span>{' '}
              {currencyFormatter.format(successOrder.totalAmount)}
            </p>
            <div className="flex justify-end gap-2 pt-2">
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  setSuccessOrder(null)
                  navigate('/orders')
                }}
              >
                Ir para lista de pedidos
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
