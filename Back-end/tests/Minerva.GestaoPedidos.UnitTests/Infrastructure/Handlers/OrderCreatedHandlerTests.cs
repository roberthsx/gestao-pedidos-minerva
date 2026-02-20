using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Handlers;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Handlers;

/// <summary>
/// Testes do OrderCreatedHandler: criação de DeliveryTerm, idempotência e pedido não encontrado.
/// </summary>
public class OrderCreatedHandlerTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OrderCreatedHandler_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ProcessAsync_WhenOrderExists_CreatesDeliveryTerm()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var customer = new Customer("C", "c@c.com");
        var payment = new PaymentCondition("À vista", 1);
        ctx.Customers.Add(customer);
        ctx.PaymentConditions.Add(payment);
        await ctx.SaveChangesAsync();
        var order = Order.Create(customer.Id, payment.Id, new DateTime(2026, 2, 18, 12, 0, 0, DateTimeKind.Utc), new[] { ("P", 1, 10m) });
        order.SetIdempotencyKey("key-" + Guid.NewGuid().ToString("N"));
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var logger = new Mock<ILogger<OrderCreatedHandler>>();
        var sut = new OrderCreatedHandler(ctx, logger.Object);
        await sut.ProcessAsync(order.Id, "corr-1", CancellationToken.None);

        var deliveryTerm = await ctx.DeliveryTerms.FirstOrDefaultAsync(d => d.OrderId == order.Id);
        deliveryTerm.Should().NotBeNull();
        deliveryTerm!.DeliveryDays.Should().Be(10);
    }

    [Fact]
    public async Task ProcessAsync_WhenCalledTwice_IsIdempotent()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var customer = new Customer("C", "c@c.com");
        var payment = new PaymentCondition("À vista", 1);
        ctx.Customers.Add(customer);
        ctx.PaymentConditions.Add(payment);
        await ctx.SaveChangesAsync();
        var order = Order.Create(customer.Id, payment.Id, DateTime.UtcNow, new[] { ("P", 1, 10m) });
        order.SetIdempotencyKey("key-" + Guid.NewGuid().ToString("N"));
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var logger = new Mock<ILogger<OrderCreatedHandler>>();
        var sut = new OrderCreatedHandler(ctx, logger.Object);
        await sut.ProcessAsync(order.Id, "corr-1", CancellationToken.None);
        await sut.ProcessAsync(order.Id, "corr-2", CancellationToken.None);

        var count = await ctx.DeliveryTerms.CountAsync(d => d.OrderId == order.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_WhenOrderNotFound_ThrowsInvalidOperationException()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var logger = new Mock<ILogger<OrderCreatedHandler>>();
        var sut = new OrderCreatedHandler(ctx, logger.Object);

        var act = () => sut.ProcessAsync(999, "corr-1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*999*não encontrado*");
    }

    [Fact]
    public async Task ProcessAsync_WhenOrderIdZero_ThrowsArgumentException()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var logger = new Mock<ILogger<OrderCreatedHandler>>();
        var sut = new OrderCreatedHandler(ctx, logger.Object);

        var act = () => sut.ProcessAsync(0, null, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
