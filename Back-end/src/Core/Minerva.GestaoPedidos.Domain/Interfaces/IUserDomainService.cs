namespace Minerva.GestaoPedidos.Domain.Interfaces;

/// <summary>
/// Serviço de domínio para regras de usuário que dependem de persistência (ex.: unicidade de e-mail).
/// </summary>
public interface IUserDomainService
{
    /// <summary>
    /// Valida que o e-mail é único. Lança exceção quando o e-mail já está em uso.
    /// </summary>
    Task ValidateUniqueEmailAsync(string email, CancellationToken cancellationToken = default);
}