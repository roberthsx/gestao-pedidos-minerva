using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Testes da regra de prazo de entrega e guards (orderId <= 0, deliveryDays <= 0).
/// </summary>
public class DeliveryTermTests
{
    private const int DeliveryDaysRequisito = 10;

    [Fact]
    public void Constructor_WhenOrderIdZero_ThrowsArgumentException()
    {
        var orderDate = new DateTime(2026, 2, 17, 0, 0, 0, DateTimeKind.Utc);
        Action act = () => new DeliveryTerm(0, orderDate, 10);
        act.Should().Throw<ArgumentException>().WithParameterName("orderId").WithMessage("*Order id is required*");
    }

    [Fact]
    public void Constructor_WhenOrderIdNegative_ThrowsArgumentException()
    {
        var orderDate = new DateTime(2026, 2, 17, 0, 0, 0, DateTimeKind.Utc);
        Action act = () => new DeliveryTerm(-1, orderDate, 10);
        act.Should().Throw<ArgumentException>().WithParameterName("orderId");
    }

    [Fact]
    public void Constructor_WhenDeliveryDaysZero_ThrowsArgumentOutOfRangeException()
    {
        var orderDate = new DateTime(2026, 2, 17, 0, 0, 0, DateTimeKind.Utc);
        Action act = () => new DeliveryTerm(1, orderDate, 0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("deliveryDays").WithMessage("*Delivery days must be greater than zero*");
    }

    [Fact]
    public void Constructor_WhenDeliveryDaysNegative_ThrowsArgumentOutOfRangeException()
    {
        var orderDate = new DateTime(2026, 2, 17, 0, 0, 0, DateTimeKind.Utc);
        Action act = () => new DeliveryTerm(1, orderDate, -1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("deliveryDays");
    }

    [Fact]
    public void Constructor_WhenOrderDateAnd10Days_ShouldSetEstimatedDeliveryDateExactly10DaysAfterOrderDate()
    {
        // Arrange: data do pedido fixa para evitar flakiness
        var orderDate = new DateTime(2026, 2, 17, 14, 30, 0, DateTimeKind.Utc);
        int orderId = 1;

        // Act: mesma lógica do Worker (DeliveryDays = 10)
        var deliveryTerm = new DeliveryTerm(orderId, orderDate, DeliveryDaysRequisito);

        // Assert
        var expectedDate = orderDate.AddDays(DeliveryDaysRequisito);
        deliveryTerm.EstimatedDeliveryDate.Should().Be(expectedDate,
            "a data estimada de entrega deve ser exatamente 10 dias após a data do pedido");
        deliveryTerm.DeliveryDays.Should().Be(DeliveryDaysRequisito);
        deliveryTerm.OrderId.Should().Be(orderId);
    }

    [Theory]
    [InlineData(2026, 2, 17, 2026, 2, 27)]
    [InlineData(2026, 1, 22, 2026, 2, 1)]
    public void Constructor_With10Days_EstimatedDeliveryDateIsOrderDatePlus10(
        int orderYear, int orderMonth, int orderDay,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var orderDate = new DateTime(orderYear, orderMonth, orderDay, 0, 0, 0, DateTimeKind.Utc);
        var deliveryTerm = new DeliveryTerm(1, orderDate, DeliveryDaysRequisito);

        deliveryTerm.EstimatedDeliveryDate.Year.Should().Be(expectedYear);
        deliveryTerm.EstimatedDeliveryDate.Month.Should().Be(expectedMonth);
        deliveryTerm.EstimatedDeliveryDate.Day.Should().Be(expectedDay);
    }
}
