using System.Text.Json.Serialization;

namespace Minerva.GestaoPedidos.Application.DTOs;

/// <summary>
/// Login credentials: registration number and password.
/// Nullable so the controller can handle empty payload and return a localized message.
/// </summary>
public record LoginRequestDto(
    string? RegistrationNumber,
    [property: JsonPropertyName("senha")] string? Password);
