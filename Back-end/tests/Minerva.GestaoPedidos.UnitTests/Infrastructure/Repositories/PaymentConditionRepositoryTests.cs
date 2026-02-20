using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes de persistência e consulta do PaymentConditionRepository sobre banco em memória.
/// </summary>
public class PaymentConditionRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("PaymentCondRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAndReturnPaymentCondition()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new PaymentConditionRepository(ctx);
        var pc = new PaymentCondition("30 dias", 1);

        var added = await repo.AddAsync(pc, CancellationToken.None);

        added.Should().BeSameAs(pc);
        added.Id.Should().BeGreaterThan(0);
        var found = await repo.GetByIdAsync(added.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Description.Should().Be("30 dias");
        found.NumberOfInstallments.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new PaymentConditionRepository(ctx);

        var result = await repo.GetByIdAsync(99999, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new PaymentConditionRepository(ctx);

        var result = await repo.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenHasConditions_ReturnsAll()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new PaymentConditionRepository(ctx);
        await repo.AddAsync(new PaymentCondition("À vista", 1), CancellationToken.None);
        await repo.AddAsync(new PaymentCondition("30 dias", 1), CancellationToken.None);

        var result = await repo.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
