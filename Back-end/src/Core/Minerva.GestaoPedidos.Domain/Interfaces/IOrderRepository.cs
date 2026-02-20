using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Obtém o pedido pelo hash de idempotência (para retornar o Id existente em caso de duplicata).</summary>
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    /// <summary>Obtém o pedido com Customer e PaymentCondition para montagem do DTO de resposta.</summary>
    Task<Order?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    /// <summary>Persiste o pedido aprovado (Status atualizado). Publicação no Kafka é feita pelo handler.</summary>
    Task SaveApprovedOrderAsync(Order order, CancellationToken cancellationToken = default);
}