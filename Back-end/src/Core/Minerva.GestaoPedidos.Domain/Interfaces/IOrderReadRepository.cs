using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.ReadModels;

namespace Minerva.GestaoPedidos.Domain.Interfaces;

public interface IOrderReadRepository
{
    Task<(IReadOnlyList<OrderReadModel> Items, int TotalCount)> GetPagedAsync(
        OrderStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}