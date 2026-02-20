using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.Infrastructure.Data;
using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Infrastructure.Repositories;

/// <summary>
/// Listagem de pedidos (GET /api/orders) a partir do PostgreSQL com AsNoTracking e Include para performance.
/// </summary>
[ExcludeFromCodeCoverage]
public class OrderReadRepository : IOrderReadRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public OrderReadRepository(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(IReadOnlyList<OrderReadModel> Items, int TotalCount)> GetPagedAsync(
        OrderStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
            pageNumber = 1;
        if (pageSize <= 0)
            pageSize = 20;

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .Include(o => o.PaymentCondition)
            .Include(o => o.DeliveryTerm)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (dateFrom.HasValue)
            query = query.Where(o => o.OrderDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(o => o.OrderDate <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = orders.Select(o => _mapper.Map<OrderReadModel>(o)).ToList();
        return (items, totalCount);
    }
}