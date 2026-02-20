using Minerva.GestaoPedidos.Application.Common;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Serviço de autenticação: valida matrícula/senha, identifica perfil internamente e emite JWT (com cache).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica por número de registro e senha; perfil é identificado pela API (não enviado pelo cliente).
    /// Retorna token do cache se válido; caso contrário gera novo JWT, armazena no cache e retorna.
    /// </summary>
    Task<Result<LoginResultDto>> LoginAsync(
        string registrationNumber,
        string senha,
        CancellationToken cancellationToken = default);
}