using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Publica o evento de aprovação no tópico order-approved para processamento assíncrono (ex.: Worker).
/// A infraestrutura converte a entidade para o formato externo (ACL); a Application não vê contratos de terceiros.
/// </summary>
public interface IOrderApprovedPublisher
{
    /// <summary>
    /// Publica o pedido aprovado no tópico order-approved. Retorna true se enviado; false se broker indisponível.
    /// </summary>
    /// <param name="order">Entidade do pedido aprovado (domínio).</param>
    Task<bool> PublishOrderApprovedAsync(Order order, CancellationToken cancellationToken = default);
}