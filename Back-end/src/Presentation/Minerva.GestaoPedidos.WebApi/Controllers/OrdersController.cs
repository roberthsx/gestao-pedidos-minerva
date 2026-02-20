using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.ApproveOrder;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.CreateOrder;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Queries.GetOrdersPaged;
using Minerva.GestaoPedidos.Domain.Constants;
using Minerva.GestaoPedidos.WebApi.Middleware;

namespace Minerva.GestaoPedidos.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private const string ClaimMatricula = "matricula";

    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Cria pedido. Apenas ADMIN e MANAGER (403 Forbidden para ANALYST).</summary>
    [HttpPost]
    [Authorize(Roles = ApplicationRoles.CreateOrderRoles)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var cmd = command with
        {
            CorrelationId = HttpContext.Items[CorrelationMiddleware.CorrelationIdKey] as string,
            CausationId = HttpContext.Items[CorrelationMiddleware.CausationIdKey] as string
        };
        var created = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(GetPaged), new { id = created.Id }, created);
    }

    /// <summary>Listagem paginada (PostgreSQL). Contrato: items, totalCount, pageNumber, pageSize.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetPaged(
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetOrdersPagedQuery(status, dateFrom, dateTo, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Aprova pedido. ADMIN, MANAGER e ANALYST. Auditoria: ApprovedBy (matrícula) e ApprovedAt.</summary>
    [HttpPut("{orderId:int}/approve")]
    [Authorize(Roles = ApplicationRoles.ApproveOrderRoles)]
    public async Task<ActionResult<OrderDto>> Approve(int orderId, CancellationToken cancellationToken)
    {
        var approvedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(ClaimMatricula)?.Value;

        var result = await _mediator.Send(new ApproveOrderCommand(orderId, approvedBy), cancellationToken);
        return Ok(result);
    }
}