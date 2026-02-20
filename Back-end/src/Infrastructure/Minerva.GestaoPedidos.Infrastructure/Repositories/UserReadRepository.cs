using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Minerva.GestaoPedidos.Infrastructure.Data;

namespace Minerva.GestaoPedidos.Infrastructure.Repositories;

/// <summary>
/// Consultas de usuário do lado de leitura usando PostgreSQL (AppDbContext). AsNoTracking para performance.
/// Usa AutoMapper para User -> UserReadModel (consistente com Handlers).
/// </summary>
[ExcludeFromCodeCoverage]
public class UserReadRepository : IUserReadRepository
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public UserReadRepository(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserReadModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user == null ? null : _mapper.Map<UserReadModel>(user);
    }

    public async Task<IReadOnlyList<UserReadModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return users.Select(u => _mapper.Map<UserReadModel>(u)).ToList();
    }
}
