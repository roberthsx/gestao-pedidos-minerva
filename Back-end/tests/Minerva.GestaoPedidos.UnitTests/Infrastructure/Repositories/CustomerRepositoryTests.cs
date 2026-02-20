using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes de persistência e consulta do CustomerRepository sobre banco em memória.
/// </summary>
public class CustomerRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("CustomerRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAndReturnCustomer()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new CustomerRepository(ctx);
        var customer = new Customer("Minerva Foods", "contato@minerva.com");

        var added = await repo.AddAsync(customer, CancellationToken.None);

        added.Should().BeSameAs(customer);
        added.Id.Should().BeGreaterThan(0);
        var found = await repo.GetByIdAsync(added.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Minerva Foods");
        found.Email.Should().Be("contato@minerva.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new CustomerRepository(ctx);

        var result = await repo.GetByIdAsync(99999, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new CustomerRepository(ctx);

        var result = await repo.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenHasCustomers_ReturnsAll()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var repo = new CustomerRepository(ctx);
        await repo.AddAsync(new Customer("A", "a@a.com"), CancellationToken.None);
        await repo.AddAsync(new Customer("B", "b@b.com"), CancellationToken.None);

        var result = await repo.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
