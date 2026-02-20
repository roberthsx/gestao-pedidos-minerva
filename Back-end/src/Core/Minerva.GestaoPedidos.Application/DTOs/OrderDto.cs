namespace Minerva.GestaoPedidos.Application.DTOs;

public record OrderDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int PaymentConditionId,
    string PaymentConditionDescription,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    bool RequiresManualApproval,
    int DeliveryDays,
    DateTime? EstimatedDeliveryDate,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    IReadOnlyCollection<OrderItemDto> Items);
