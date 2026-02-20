using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;

namespace Minerva.GestaoPedidos.Tests.Fakes;

/// <summary>
/// Fake adapter que usa IUserRepository (escrita) como leitura para testes em memória.
/// Não faz parte do código de produção.
/// </summary>
public class InMemoryUserReadRepositoryAdapter : IUserReadRepository
{
    private readonly IUserRepository _writeRepository;

    public InMemoryUserReadRepositoryAdapter(IUserRepository writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<UserReadModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _writeRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return null;

        return new UserReadModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Active = user.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<UserReadModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _writeRepository.GetAllAsync(cancellationToken);
        return users
            .Select(u => new UserReadModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Active = u.Active,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();
    }
}
