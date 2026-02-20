using MediatR;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Domain.Events;

/// <summary>
/// Evento de domínio disparado quando um pedido é aprovado manualmente (status Criado -> Pago).
/// Publicado no Kafka (order-approved) pelo handler para processamento assíncrono.
/// </summary>
public sealed class OrderApprovedEvent : INotification
{
    public OrderApprovedEvent(int orderId, OrderStatus status, DateTime approvedAtUtc)
    {
        OrderId = orderId;
        Status = status;
        ApprovedAtUtc = approvedAtUtc;
    }

    public int OrderId { get; }
    public OrderStatus Status { get; }
    public DateTime ApprovedAtUtc { get; }
}
