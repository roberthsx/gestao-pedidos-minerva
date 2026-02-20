using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.Infrastructure.Data;

namespace Minerva.GestaoPedidos.Infrastructure.Repositories;

/// <summary>
/// Leitura de condições de pagamento com projeção para ReadModel (AsNoTracking + Select). Infrastructure não expõe DTOs.
/// </summary>
public sealed class PaymentConditionReadRepository : IPaymentConditionReadRepository
{
    private readonly AppDbContext _context;

    public PaymentConditionReadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PaymentConditionReadModel>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentConditions
            .AsNoTracking()
            .OrderBy(p => p.Description)
            .Select(p => new PaymentConditionReadModel { Id = p.Id, Description = p.Description, NumberOfInstallments = p.NumberOfInstallments })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
