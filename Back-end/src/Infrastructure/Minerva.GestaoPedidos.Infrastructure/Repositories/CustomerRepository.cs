using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Infrastructure.Data;
using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }
}