using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Domain.Interfaces;

public interface IPaymentConditionRepository
{
    Task<PaymentCondition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentCondition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaymentCondition> AddAsync(PaymentCondition paymentCondition, CancellationToken cancellationToken = default);
}