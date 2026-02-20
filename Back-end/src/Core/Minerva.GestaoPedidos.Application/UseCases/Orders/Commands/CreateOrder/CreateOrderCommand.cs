using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.CreateOrder;

public record CreateOrderItemCommand(
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record CreateOrderCommand(
    int CustomerId,
    int PaymentConditionId,
    DateTime? OrderDate,
    IReadOnlyCollection<CreateOrderItemCommand> Items,
    string? CorrelationId = null,
    string? CausationId = null) : IRequest<OrderDto>;