namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Processa uma mensagem order-created: cria DeliveryTerm no Postgres (idempotente).
/// Resolvido por escopo no Worker; implementação na Infrastructure.
/// </summary>
public interface IOrderCreatedMessageHandler
{
    /// <summary>
    /// Cria DeliveryTerm para o pedido quando ainda não existir. Idempotente por OrderId.
    /// </summary>
    Task ProcessAsync(int orderId, string? correlationId, CancellationToken cancellationToken = default);
}
