using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Application.Common.Mappings;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes do OrderReadRepository com banco em memória: mapeamento via AutoMapper e busca paginada.
/// </summary>
public class OrderReadRepositoryTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OrderReadRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetPagedAsync_EmptyDb_ReturnsZeroItemsAndZeroTotal()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var sut = new OrderReadRepository(ctx, CreateMapper());

        var (items, totalCount) = await sut.GetPagedAsync(null, null, null, 1, 20, CancellationToken.None);

        items.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_WithOrder_MapToReadModel_ReturnsCorrectCustomerNameAndItems()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var customer = new Customer("Minerva Foods", "contato@minerva.com");
        var payment = new PaymentCondition("À vista", 1);
        ctx.Customers.Add(customer);
        ctx.PaymentConditions.Add(payment);
        await ctx.SaveChangesAsync();

        var order = Order.Create(customer.Id, payment.Id, new DateTime(2026, 2, 11, 10, 0, 0, DateTimeKind.Utc), new[] { ("Arroz", 2, 25.50m), ("Feijão", 1, 8m) });
        order.SetIdempotencyKey("key-" + Guid.NewGuid().ToString("N"));
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var sut = new OrderReadRepository(ctx, CreateMapper());
        var (items, totalCount) = await sut.GetPagedAsync(null, null, null, 1, 10, CancellationToken.None);

        totalCount.Should().Be(1);
        items.Should().HaveCount(1);
        var read = items[0];
        read.OrderId.Should().Be(order.Id);
        read.CustomerId.Should().Be(customer.Id);
        read.CustomerName.Should().Be("Minerva Foods");
        read.PaymentConditionDescription.Should().Be("À vista");
        read.TotalAmount.Should().Be(59m); // 2*25.50 + 8
        read.Status.Should().Be(order.Status.ToString());
        read.Items.Should().HaveCount(2);
        read.Items[0].ProductName.Should().Be("Arroz");
        read.Items[0].Quantity.Should().Be(2);
        read.Items[0].UnitPrice.Should().Be(25.50m);
        read.Items[0].TotalPrice.Should().Be(51m);
    }

    [Fact]
    public async Task GetPagedAsync_WhenPageNumberOrPageSizeZero_NormalizesToDefaults()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var sut = new OrderReadRepository(ctx, CreateMapper());

        var (_, totalCount) = await sut.GetPagedAsync(null, null, null, 0, 0, CancellationToken.None);

        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedAsync_WithStatusFilter_ReturnsOnlyMatchingOrders()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var customer = new Customer("C", "c@c.com");
        var payment = new PaymentCondition("À vista", 1);
        ctx.Customers.Add(customer);
        ctx.PaymentConditions.Add(payment);
        await ctx.SaveChangesAsync();

        var orderPago = Order.Create(customer.Id, payment.Id, DateTime.UtcNow, new[] { ("P", 1, 10m) });
        orderPago.SetIdempotencyKey("k1");
        ctx.Orders.Add(orderPago);
        await ctx.SaveChangesAsync();

        var sut = new OrderReadRepository(ctx, CreateMapper());
        var (items, totalCount) = await sut.GetPagedAsync(OrderStatus.Pago, null, null, 1, 10, CancellationToken.None);

        totalCount.Should().Be(1);
        items[0].Status.Should().Be("Pago");
    }
}
