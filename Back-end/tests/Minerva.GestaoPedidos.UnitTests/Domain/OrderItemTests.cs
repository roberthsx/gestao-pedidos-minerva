using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Constructor_WhenOrderIdNegative_ThrowsArgumentException()
    {
        Action act = () => new OrderItem(-1, "Produto", 1, 10m);
        act.Should().Throw<ArgumentException>().WithParameterName("orderId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenProductNameNullOrWhiteSpace_ThrowsArgumentException(string? productName)
    {
        Action act = () => new OrderItem(1, productName!, 1, 10m);
        act.Should().Throw<ArgumentException>().WithParameterName("productName");
    }

    [Fact]
    public void Constructor_WhenQuantityZero_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new OrderItem(1, "Produto", 0, 10m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quantity");
    }

    [Fact]
    public void Constructor_WhenQuantityNegative_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new OrderItem(1, "Produto", -1, 10m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quantity");
    }

    [Fact]
    public void Constructor_WhenUnitPriceZero_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new OrderItem(1, "Produto", 1, 0m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unitPrice");
    }

    [Fact]
    public void Constructor_WhenUnitPriceNegative_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new OrderItem(1, "Produto", 1, -0.01m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unitPrice");
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsPropertiesAndComputesTotalPrice()
    {
        var item = new OrderItem(1, "  Arroz  ", 3, 10.50m);
        item.OrderId.Should().Be(1);
        item.ProductName.Should().Be("Arroz");
        item.Quantity.Should().Be(3);
        item.UnitPrice.Should().Be(10.50m);
        item.TotalPrice.Should().Be(31.50m);
    }
}
