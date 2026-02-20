using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Publica o pedido no tópico order-created para processamento assíncrono (cálculo de entrega).
/// Implementação resiliente: falha na publicação não invalida a criação do pedido.
/// A infraestrutura converte a entidade para o formato externo (ACL); a Application não vê contratos de terceiros.
/// </summary>
public interface IOrderCreatedPublisher
{
    /// <summary>
    /// Publica o pedido no tópico order-created. Retorna true se enviado; false se broker indisponível.
    /// </summary>
    /// <param name="order">Entidade do pedido criado (domínio).</param>
    /// <param name="correlationId">Correlation ID para rastreio (ex.: do header X-Correlation-ID).</param>
    /// <param name="causationId">Causation ID para rastreio (ex.: do header X-Causation-ID).</param>
    Task<bool> PublishOrderCreatedAsync(Order order, string? correlationId, string? causationId, CancellationToken cancellationToken = default);
}