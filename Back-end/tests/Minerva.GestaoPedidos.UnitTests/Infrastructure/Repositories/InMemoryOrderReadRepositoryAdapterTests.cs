using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Application.Common.Mappings;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Tests.Fakes;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes para InMemoryOrderReadRepositoryAdapter: paginação e filtros sobre banco em memória.
/// </summary>
public class InMemoryOrderReadRepositoryAdapterTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("InMemRead_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_EmptyDb_ReturnsZeroItemsAndZeroTotal()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var sut = new InMemoryOrderReadRepositoryAdapter(ctx, CreateMapper());

        var (items, totalCount) = await sut.GetPagedAsync(null, null, null, 1, 10, CancellationToken.None);

        items.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_WithOrders_ReturnsPaginatedResults()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var customer = new Customer("Cliente", "c@c.com");
        var payment = new PaymentCondition("À vista", 1);
        ctx.Customers.Add(customer);
        ctx.PaymentConditions.Add(payment);
        await ctx.SaveChangesAsync();

        var order = Order.Create(customer.Id, payment.Id, DateTime.UtcNow, new[] { ("P1", 2, 50m) });
        order.SetIdempotencyKey("key1");
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var sut = new InMemoryOrderReadRepositoryAdapter(ctx, CreateMapper());

        var (items, totalCount) = await sut.GetPagedAsync(null, null, null, 1, 10, CancellationToken.None);

        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].OrderId.Should().Be(order.Id);
        items[0].TotalAmount.Should().Be(100m);
        items[0].CustomerName.Should().Be("Cliente");
    }

    [Fact]
    public async Task GetPagedAsync_WhenPageNumberZero_DefaultsToPageOne()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var sut = new InMemoryOrderReadRepositoryAdapter(ctx, CreateMapper());

        var (items, _) = await sut.GetPagedAsync(null, null, null, 0, 10, CancellationToken.None);

        items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_WhenStatusFilter_ReturnsOnlyMatchingStatus()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var customer = new Customer("C", "c@c.com");
        var payment = new PaymentCondition("À vista", 1);
        ctx.Customers.Add(customer);
        ctx.PaymentConditions.Add(payment);
        await ctx.SaveChangesAsync();

        var orderCriado = Order.Create(customer.Id, payment.Id, DateTime.UtcNow, new[] { ("X", 100, 100m) });
        orderCriado.SetIdempotencyKey("k1");
        ctx.Orders.Add(orderCriado);
        var orderPago = Order.Create(customer.Id, payment.Id, DateTime.UtcNow.AddDays(-1), new[] { ("Y", 1, 10m) });
        orderPago.SetIdempotencyKey("k2");
        ctx.Orders.Add(orderPago);
        await ctx.SaveChangesAsync();

        var sut = new InMemoryOrderReadRepositoryAdapter(ctx, CreateMapper());

        var (items, totalCount) = await sut.GetPagedAsync(OrderStatus.Criado, null, null, 1, 10, CancellationToken.None);

        totalCount.Should().Be(1);
        items.Should().ContainSingle().Which.Status.Should().Be(nameof(OrderStatus.Criado));
    }
}
