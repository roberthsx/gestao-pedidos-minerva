namespace Minerva.GestaoPedidos.Application.Common.Exceptions;

/// <summary>
/// Lançada quando uma tentativa de criar pedido viola a constraint única de idempotência (mesmo hash de negócio).
/// Mapeada para HTTP 409 Conflict com existingOrderId no corpo para o cliente retornar "Pedido já processado".
/// </summary>
public class OrderAlreadyExistsException : Exception
{
    public int ExistingOrderId { get; }

    public OrderAlreadyExistsException(int existingOrderId)
        : base("Este pedido já foi processado anteriormente.")
    {
        ExistingOrderId = existingOrderId;
    }
}