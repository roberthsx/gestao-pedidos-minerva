using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Constants;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Infrastructure.Identity.Services;

/// <summary>
/// Store de autenticação que busca usuário no Postgres (registro, senha hash, nome, perfil).
/// Mapeia Profile.Code (ADMIN, GESTAO, ANALISTA) para Role do JWT (MANAGER, ANALYST).
/// </summary>
public sealed class DbAuthUserStore : IAuthUserStore
{
    private readonly IUserRepository _userRepository;

    public DbAuthUserStore(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthUserInfo?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRegistrationNumberAsync(registrationNumber, cancellationToken);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        var nome = $"{user.FirstName} {user.LastName}".Trim();
        var perfil = MapProfileCodeToRole(user.Profile?.Code);

        return new AuthUserInfo(user.RegistrationNumber!, user.PasswordHash, nome, perfil);
    }

    private static string MapProfileCodeToRole(string? profileCode)
    {
        if (string.IsNullOrWhiteSpace(profileCode))
            return ApplicationRoles.Analyst;

        var code = profileCode.Trim().ToUpperInvariant();
        if (code == ProfileCodes.Admin)
            return ApplicationRoles.Admin;
        if (code == ProfileCodes.Gestao)
            return ApplicationRoles.Manager;
        return ApplicationRoles.Analyst;
    }
}
