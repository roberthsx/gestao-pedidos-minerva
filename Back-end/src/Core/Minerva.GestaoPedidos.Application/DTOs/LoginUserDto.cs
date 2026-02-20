namespace Minerva.GestaoPedidos.Application.DTOs;

/// <summary>
/// User data returned in the login response (name and role identified by the API).
/// </summary>
public record LoginUserDto(string Name, string Role);
