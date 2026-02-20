using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Npgsql;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Handlers;

/// <summary>
/// Cria DeliveryTerm para o pedido a partir da mensagem order-created. Idempotente por OrderId.
/// </summary>
public sealed class OrderCreatedHandler : IOrderCreatedMessageHandler
{
    private const int DeliveryDays = 10;
    private readonly AppDbContext _db;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(AppDbContext db, ILogger<OrderCreatedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(int orderId, string? correlationId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
            throw new ArgumentException("OrderId inválido.", nameof(orderId));

        var exists = await _db.DeliveryTerms
            .AnyAsync(d => d.OrderId == orderId, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            _logger.LogWarning("Idempotência acionada: OrderId {OrderId} já possui DeliveryTerm. CorrelationId: {CorrId}.", orderId, correlationId ?? "(n/a)");
            return;
        }

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
            throw new InvalidOperationException($"Pedido {orderId} não encontrado.");

        var orderDateUtc = order.OrderDate.Kind == DateTimeKind.Utc
            ? order.OrderDate
            : order.OrderDate.ToUniversalTime();
        var deliveryTerm = new DeliveryTerm(orderId, orderDateUtc, DeliveryDays);
        _db.DeliveryTerms.Add(deliveryTerm);

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            _logger.LogWarning("Idempotência acionada: conflito único. OrderId {OrderId}. CorrelationId: {CorrId}.", orderId, correlationId ?? "(n/a)");
            return;
        }

        _logger.LogInformation("DeliveryTerm criado para o pedido {OrderId} (DeliveryDays={Days}).", orderId, DeliveryDays);
    }
}
