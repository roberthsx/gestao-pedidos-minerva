using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.Infrastructure.Data;

namespace Minerva.GestaoPedidos.Tests.Fakes;

/// <summary>
/// Fake adapter que usa o DbContext em memória para leitura de pedidos (testes de integração).
/// Usa o mesmo AutoMapper que OrderReadRepository (Order -> OrderReadModel) para consistência.
/// </summary>
public class InMemoryOrderReadRepositoryAdapter : IOrderReadRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public InMemoryOrderReadRepositoryAdapter(AppDbContext context, IMapper mapper)
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
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.PaymentCondition)
            .Include(o => o.DeliveryTerm)
            .Include(o => o.Items)
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
