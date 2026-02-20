using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Domain.Constants;

/// <summary>
/// Roles usadas no JWT e em [Authorize(Roles = "...")].
/// RBAC: ADMIN e MANAGER = acesso total (criar + aprovar); ANALYST = apenas aprovar.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApplicationRoles
{
    /// <summary>Administrador: acesso total (criar e aprovar pedidos).</summary>
    public const string Admin = "ADMIN";

    /// <summary>Gestão: acesso total (criar e aprovar pedidos).</summary>
    public const string Manager = "MANAGER";

    /// <summary>Analista: apenas aprovar pedidos (não pode criar).</summary>
    public const string Analyst = "ANALYST";

    /// <summary>Roles que podem criar pedidos (POST).</summary>
    public const string CreateOrderRoles = Admin + "," + Manager;

    /// <summary>Roles que podem aprovar pedidos (PUT approve).</summary>
    public const string ApproveOrderRoles = Admin + "," + Manager + "," + Analyst;
}