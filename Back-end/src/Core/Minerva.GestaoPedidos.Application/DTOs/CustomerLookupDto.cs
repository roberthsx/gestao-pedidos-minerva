using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Application.DTOs;

/// <summary>
/// DTO de lookup para seleção de cliente (dropdowns no front-end).
/// </summary>
[ExcludeFromCodeCoverage]
public record CustomerLookupDto(int Id, string Name);