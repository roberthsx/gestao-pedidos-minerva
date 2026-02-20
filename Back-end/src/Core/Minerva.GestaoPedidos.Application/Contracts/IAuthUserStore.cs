namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Store de usuários para autenticação: busca por matrícula (PasswordHash, Name, Role).
/// Pode ser implementado com dicionário estático ou busca no Postgres.
/// </summary>
public interface IAuthUserStore
{
    /// <summary>
    /// Obtém dados do usuário por número de registro. Perfil (role no JWT) deve ser ApplicationRoles.Manager ou ApplicationRoles.Analyst.
    /// </summary>
    Task<AuthUserInfo?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dados do usuário para validação de senha e emissão do token (role no JWT).
/// </summary>
public record AuthUserInfo(string RegistrationNumber, string PasswordHash, string Name, string Role);