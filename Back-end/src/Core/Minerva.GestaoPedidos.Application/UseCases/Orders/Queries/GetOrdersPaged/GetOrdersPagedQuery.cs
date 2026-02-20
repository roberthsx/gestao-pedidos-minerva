using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Queries.GetOrdersPaged;

public record GetOrdersPagedQuery(
    string? Status,
    DateTime? DateFrom,
    DateTime? DateTo,
    int PageNumber,
    int PageSize) : IRequest<PagedResponse<OrderDto>>;