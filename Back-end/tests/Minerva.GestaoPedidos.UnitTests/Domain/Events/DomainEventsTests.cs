using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Events;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain.Events;

/// <summary>
/// Testes para eventos de domínio: criação e propriedades de OrderApprovedEvent, OrderCreatedEvent e OrderCreatedEventItem.
/// </summary>
public class DomainEventsTests
{
    [Fact]
    public void OrderApprovedEvent_ShouldSetPropertiesCorrectly()
    {
        var approvedAt = new DateTime(2026, 2, 20, 14, 30, 0, DateTimeKind.Utc);
        var evt = new OrderApprovedEvent(42, OrderStatus.Pago, approvedAt);

        evt.OrderId.Should().Be(42);
        evt.Status.Should().Be(OrderStatus.Pago);
        evt.ApprovedAtUtc.Should().Be(approvedAt);
    }

    [Fact]
    public void OrderCreatedEvent_ShouldSetPropertiesAndDefaultEmptyItems()
    {
        var createdAt = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);
        var evt = new OrderCreatedEvent(
            orderId: 1,
            customerId: 10,
            paymentConditionId: 2,
            totalAmount: 5000m,
            status: OrderStatus.Pago,
            requiresManualApproval: false,
            createdAtUtc: createdAt,
            items: null);

        evt.OrderId.Should().Be(1);
        evt.CustomerId.Should().Be(10);
        evt.PaymentConditionId.Should().Be(2);
        evt.TotalAmount.Should().Be(5000m);
        evt.Status.Should().Be(OrderStatus.Pago);
        evt.RequiresManualApproval.Should().BeFalse();
        evt.CreatedAtUtc.Should().Be(createdAt);
        evt.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void OrderCreatedEvent_WithItems_ShouldExposeItems()
    {
        var items = new List<OrderCreatedEventItem>
        {
            new("Produto A", 2, 100m, 200m)
        };
        var evt = new OrderCreatedEvent(1, 1, 1, 200m, OrderStatus.Criado, true, DateTime.UtcNow, items);

        evt.Items.Should().HaveCount(1);
        evt.Items[0].ProductName.Should().Be("Produto A");
        evt.Items[0].Quantity.Should().Be(2);
        evt.Items[0].UnitPrice.Should().Be(100m);
        evt.Items[0].TotalPrice.Should().Be(200m);
    }

    [Fact]
    public void OrderCreatedEventItem_Record_ShouldHoldValues()
    {
        var item = new OrderCreatedEventItem("X", 3, 50m, 150m);

        item.ProductName.Should().Be("X");
        item.Quantity.Should().Be(3);
        item.UnitPrice.Should().Be(50m);
        item.TotalPrice.Should().Be(150m);
    }
}
