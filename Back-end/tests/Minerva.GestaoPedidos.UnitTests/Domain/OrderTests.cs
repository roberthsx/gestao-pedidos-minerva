using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Testes das regras de negócio da entidade Order (domínio core Minerva Foods).
/// </summary>
public class OrderTests
{
    private static readonly int CustomerId = 1;
    private static readonly int PaymentConditionId = 1;
    private static readonly DateTime OrderDate = new(2026, 2, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WhenTotalAmountGreaterThan5000_ShouldSetRequiresManualApprovalTrue()
    {
        // Arrange: itens que somam > 5000 (ex.: 26 x 200 = 5200)
        var items = new List<(string ProductName, int Quantity, decimal UnitPrice)>
        {
            ("Produto A", 26, 200m)
        };

        // Act
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, items);

        // Assert
        order.TotalAmount.Should().Be(5200m);
        order.RequiresManualApproval.Should().BeTrue("pedidos com TotalAmount > 5000 exigem aprovação manual");
    }

    [Fact]
    public void Create_WhenTotalAmountLessThanOrEqual5000_ShouldSetRequiresManualApprovalFalse()
    {
        // Arrange: itens que somam <= 5000 (ex.: 25 x 200 = 5000)
        var items = new List<(string ProductName, int Quantity, decimal UnitPrice)>
        {
            ("Produto B", 25, 200m)
        };

        // Act
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, items);

        // Assert
        order.TotalAmount.Should().Be(5000m);
        order.RequiresManualApproval.Should().BeFalse("pedidos com TotalAmount <= 5000 não exigem aprovação manual");
    }

    [Fact]
    public void Create_WhenTotalAmountGreaterThan5000_ShouldSetStatusCriado()
    {
        // Arrange
        var items = new List<(string ProductName, int Quantity, decimal UnitPrice)>
        {
            ("Produto C", 30, 200m) // 6000
        };

        // Act
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, items);

        // Assert
        order.Status.Should().Be(OrderStatus.Criado);
    }

    [Fact]
    public void Create_WhenTotalAmountLessThanOrEqual5000_ShouldSetStatusPago()
    {
        // Arrange
        var items = new List<(string ProductName, int Quantity, decimal UnitPrice)>
        {
            ("Produto D", 10, 100m) // 1000
        };

        // Act
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, items);

        // Assert
        order.Status.Should().Be(OrderStatus.Pago);
    }

    [Fact]
    public void Create_WithSingleItem_InitialStatusReflectsGoldenRule()
    {
        // Arrange: valor exatamente no limite (5000)
        var items = new List<(string ProductName, int Quantity, decimal UnitPrice)>
        {
            ("Produto E", 1, 5000m)
        };

        // Act
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, items);

        // Assert: <= 5000 => Pago, sem aprovação manual
        order.TotalAmount.Should().Be(5000m);
        order.RequiresManualApproval.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Pago);
    }

    [Fact]
    public void SetIdempotencyKey_WhenNull_ThrowsArgumentException()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 1, 1m) });
        var act = () => order.SetIdempotencyKey(null!);

        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void SetIdempotencyKey_WhenEmpty_ThrowsArgumentException()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 1, 1m) });
        var act = () => order.SetIdempotencyKey("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenItemsNull_ThrowsArgumentNullException()
    {
        var act = () => Order.Create(CustomerId, PaymentConditionId, OrderDate, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Fact]
    public void Create_WhenItemsEmpty_ThrowsArgumentException()
    {
        var act = () => Order.Create(CustomerId, PaymentConditionId, OrderDate, Array.Empty<(string, int, decimal)>());

        act.Should().Throw<ArgumentException>().WithParameterName("items");
    }

    [Fact]
    public void Create_WhenCustomerIdZero_ThrowsArgumentException()
    {
        var act = () => Order.Create(0, PaymentConditionId, OrderDate, new[] { ("P", 1, 1m) });

        act.Should().Throw<ArgumentException>().WithParameterName("customerId");
    }

    [Fact]
    public void Create_WhenPaymentConditionIdZero_ThrowsArgumentException()
    {
        var act = () => Order.Create(CustomerId, 0, OrderDate, new[] { ("P", 1, 1m) });

        act.Should().Throw<ArgumentException>().WithParameterName("paymentConditionId");
    }

    [Fact]
    public void Create_WhenOrderDateDefault_ThrowsArgumentException()
    {
        DateTime? defaultDate = default(DateTime);
        var act = () => Order.Create(CustomerId, PaymentConditionId, defaultDate, new[] { ("P", 1, 1m) });

        act.Should().Throw<ArgumentException>().WithParameterName("orderDate");
    }

    [Fact]
    public void Approve_WhenAlreadyPago_ThrowsInvalidOperationException()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 1, 100m) });
        var act = () => order.Approve(null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already paid*");
    }

    [Fact]
    public void Approve_WhenCancelado_ThrowsInvalidOperationException()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 100, 100m) });
        order.Cancel();
        var act = () => order.Approve(null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
    }

    [Fact]
    public void Cancel_WhenPago_ThrowsInvalidOperationException()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 1, 100m) });
        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>().WithMessage("*paid*");
    }

    [Fact]
    public void Approve_WhenStatusCriado_SetsStatusPago()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 100, 100m) });
        order.Status.Should().Be(OrderStatus.Criado);

        order.Approve("manager1");

        order.Status.Should().Be(OrderStatus.Pago);
        order.ApprovedBy.Should().Be("manager1");
        order.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WhenStatusCriado_SetsStatusCancelado()
    {
        var order = Order.Create(CustomerId, PaymentConditionId, OrderDate, new[] { ("P", 100, 100m) });

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelado);
    }
}
