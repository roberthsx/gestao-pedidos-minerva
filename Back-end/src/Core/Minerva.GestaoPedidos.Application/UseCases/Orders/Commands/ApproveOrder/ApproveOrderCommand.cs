using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.ApproveOrder;

/// <summary>
/// Comando de aprovação de pedido. ApprovedBy (matrícula) vem dos claims do usuário autenticado.
/// </summary>
public record ApproveOrderCommand(int OrderId, string? ApprovedBy) : IRequest<OrderDto>;