using Minerva.GestaoPedidos.Domain.ReadModels;

namespace Minerva.GestaoPedidos.Domain.Interfaces;

public interface IUserReadRepository
{
    Task<UserReadModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserReadModel>> GetAllAsync(CancellationToken cancellationToken = default);
}