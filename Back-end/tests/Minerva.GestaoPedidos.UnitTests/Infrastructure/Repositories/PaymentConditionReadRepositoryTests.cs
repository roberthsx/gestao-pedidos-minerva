using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes do PaymentConditionReadRepository com banco em memória.
/// Valida que a leitura retorna os dados mapeados corretamente para PaymentConditionReadModel.
/// </summary>
public class PaymentConditionReadRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("PaymentCondRead_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetLookupAsync_WhenNoData_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new PaymentConditionReadRepository(ctx);

        var result = await repo.GetLookupAsync(CancellationToken.None);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetLookupAsync_WhenThreeRecords_ReturnsThreeReadModels()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var writeRepo = new PaymentConditionRepository(ctx);
        await writeRepo.AddAsync(new PaymentCondition("À vista", 1), CancellationToken.None);
        await writeRepo.AddAsync(new PaymentCondition("30 dias", 1), CancellationToken.None);
        await writeRepo.AddAsync(new PaymentCondition("30/60/90", 3), CancellationToken.None);

        var readRepo = new PaymentConditionReadRepository(ctx);
        var result = await readRepo.GetLookupAsync(CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Id.Should().BeGreaterThan(0);
        result.Select(d => d.Description).Should().Contain("À vista").And.Contain("30 dias").And.Contain("30/60/90");
        result.Single(d => d.Description == "30/60/90").NumberOfInstallments.Should().Be(3);
    }

    [Fact]
    public async Task GetLookupAsync_ReturnsOrderedByDescription()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var writeRepo = new PaymentConditionRepository(ctx);
        await writeRepo.AddAsync(new PaymentCondition("Zebra", 1), CancellationToken.None);
        await writeRepo.AddAsync(new PaymentCondition("Alpha", 1), CancellationToken.None);

        var readRepo = new PaymentConditionReadRepository(ctx);
        var result = await readRepo.GetLookupAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Description.Should().Be("Alpha");
        result[1].Description.Should().Be("Zebra");
    }
}
