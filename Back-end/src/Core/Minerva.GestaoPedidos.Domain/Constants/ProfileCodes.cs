using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Domain.Constants;

/// <summary>
/// Códigos dos perfis usados no JWT e em [Authorize(Roles = "...")].
/// </summary>
[ExcludeFromCodeCoverage]
public static class ProfileCodes
{
    public const string Admin = "ADMIN";
    public const string Gestao = "GESTAO";
    public const string Analista = "ANALISTA";

    public static readonly string[] All = { Admin, Gestao, Analista };

    public static bool IsValid(string? code) =>
        !string.IsNullOrWhiteSpace(code) && All.Contains(code.Trim().ToUpperInvariant());
}