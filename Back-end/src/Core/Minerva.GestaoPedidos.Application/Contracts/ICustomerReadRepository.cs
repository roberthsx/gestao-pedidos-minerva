using Minerva.GestaoPedidos.Domain.ReadModels;

namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Repositório de leitura de clientes (lookup para seleção no front-end).
/// Retorna ReadModel; a Application faz o mapeamento para DTO.
/// </summary>
public interface ICustomerReadRepository
{
    Task<IReadOnlyList<CustomerReadModel>> GetLookupAsync(CancellationToken cancellationToken = default);
}