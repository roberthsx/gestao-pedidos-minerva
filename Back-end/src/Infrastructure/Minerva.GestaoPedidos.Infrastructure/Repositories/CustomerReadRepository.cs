using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.Infrastructure.Data;

namespace Minerva.GestaoPedidos.Infrastructure.Repositories;

/// <summary>
/// Leitura de clientes com projeção para ReadModel (AsNoTracking + Select). Infrastructure não expõe DTOs.
/// </summary>
public sealed class CustomerReadRepository : ICustomerReadRepository
{
    private readonly AppDbContext _context;

    public CustomerReadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustomerReadModel>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerReadModel { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}