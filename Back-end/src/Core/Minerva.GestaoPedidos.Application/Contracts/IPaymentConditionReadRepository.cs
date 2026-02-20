using Minerva.GestaoPedidos.Domain.ReadModels;

namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Repositório de leitura de condições de pagamento (lookup para seleção no front-end).
/// Retorna ReadModel; a Application faz o mapeamento para DTO.
/// </summary>
public interface IPaymentConditionReadRepository
{
    Task<IReadOnlyList<PaymentConditionReadModel>> GetLookupAsync(CancellationToken cancellationToken = default);
}