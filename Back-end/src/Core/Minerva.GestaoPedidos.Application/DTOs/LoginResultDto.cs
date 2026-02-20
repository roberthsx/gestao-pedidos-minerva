namespace Minerva.GestaoPedidos.Application.DTOs;

/// <summary>
/// Login result: token, expiration time and user data (name and role).
/// </summary>
public record LoginResultDto(string AccessToken, int ExpiresIn, LoginUserDto User);
