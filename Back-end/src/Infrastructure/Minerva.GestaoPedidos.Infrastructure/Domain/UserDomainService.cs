using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Infrastructure.Domain;

/// <summary>
/// Implementação do serviço de domínio: garante unicidade de e-mail usando o repositório de escrita.
/// </summary>
public class UserDomainService : IUserDomainService
{
    private readonly IUserRepository _userRepository;

    public UserDomainService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task ValidateUniqueEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var exists = await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (exists)
            throw new ConflictException("The email is already in use.");
    }
}
