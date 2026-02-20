using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Application.DTOs;

/// <summary>
/// DTO de lookup para seleção de condição de pagamento (dropdowns no front-end).
/// </summary>
[ExcludeFromCodeCoverage]
public record PaymentConditionLookupDto(int Id, string Description, int NumberOfInstallments);