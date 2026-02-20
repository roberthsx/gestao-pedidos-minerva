namespace Minerva.GestaoPedidos.Application.DTOs;

/// <summary>
/// Contrato padrão de resposta paginada para o front (docs/API.md).
/// Todas as listagens paginadas retornam: items, totalCount, pageNumber, pageSize.
/// </summary>
/// <typeparam name="T">Tipo de cada item da página.</typeparam>
public record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);