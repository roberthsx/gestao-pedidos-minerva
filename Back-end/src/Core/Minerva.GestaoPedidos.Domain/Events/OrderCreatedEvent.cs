using MediatR;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Domain.Events;

/// <summary>
/// Evento de domínio disparado após a criação de um pedido no lado de escrita.
/// Destinado a sincronizar com o lado de leitura e outras integrações.
/// </summary>
public sealed class OrderCreatedEvent : INotification
{
    public OrderCreatedEvent(
        int orderId,
        int customerId,
        int paymentConditionId,
        decimal totalAmount,
        OrderStatus status,
        bool requiresManualApproval,
        DateTime createdAtUtc,
        IReadOnlyList<OrderCreatedEventItem>? items = null)
    {
        OrderId = orderId;
        CustomerId = customerId;
        PaymentConditionId = paymentConditionId;
        TotalAmount = totalAmount;
        Status = status;
        RequiresManualApproval = requiresManualApproval;
        CreatedAtUtc = createdAtUtc;
        Items = items ?? Array.Empty<OrderCreatedEventItem>();
    }

    public int OrderId { get; }
    public int CustomerId { get; }
    public int PaymentConditionId { get; }
    public decimal TotalAmount { get; }
    public OrderStatus Status { get; }
    public bool RequiresManualApproval { get; }
    public DateTime CreatedAtUtc { get; }
    public IReadOnlyList<OrderCreatedEventItem> Items { get; }
}

public sealed record OrderCreatedEventItem(string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

