using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Infrastructure.Data;
using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class PaymentConditionRepository : IPaymentConditionRepository
{
    private readonly AppDbContext _context;

    public PaymentConditionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentCondition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentConditions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentCondition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentConditions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentCondition> AddAsync(PaymentCondition paymentCondition, CancellationToken cancellationToken = default)
    {
        _context.PaymentConditions.Add(paymentCondition);
        await _context.SaveChangesAsync(cancellationToken);
        return paymentCondition;
    }
}