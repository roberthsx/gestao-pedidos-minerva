using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes do CustomerReadRepository com banco em memória.
/// Valida que a leitura retorna os dados mapeados corretamente para CustomerReadModel.
/// </summary>
public class CustomerReadRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("CustomerRead_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetLookupAsync_WhenNoData_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new CustomerReadRepository(ctx);

        var result = await repo.GetLookupAsync(CancellationToken.None);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetLookupAsync_WhenThreeRecords_ReturnsThreeReadModels()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var writeRepo = new CustomerRepository(ctx);
        await writeRepo.AddAsync(new Customer("Alpha", "alpha@test.com"), CancellationToken.None);
        await writeRepo.AddAsync(new Customer("Beta", "beta@test.com"), CancellationToken.None);
        await writeRepo.AddAsync(new Customer("Gamma", "gamma@test.com"), CancellationToken.None);

        var readRepo = new CustomerReadRepository(ctx);
        var result = await readRepo.GetLookupAsync(CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Id.Should().BeGreaterThan(0);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Beta");
        result[2].Name.Should().Be("Gamma");
    }

    [Fact]
    public async Task GetLookupAsync_ReturnsOrderedByName()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var writeRepo = new CustomerRepository(ctx);
        await writeRepo.AddAsync(new Customer("Zebra", "z@test.com"), CancellationToken.None);
        await writeRepo.AddAsync(new Customer("Alpha", "a@test.com"), CancellationToken.None);

        var readRepo = new CustomerReadRepository(ctx);
        var result = await readRepo.GetLookupAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Zebra");
    }
}
