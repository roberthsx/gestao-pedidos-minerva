using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Testes de guards do construtor de PaymentCondition (descrição nula/vazia, parcelas <= 0).
/// </summary>
public class PaymentConditionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenDescriptionNullOrWhiteSpace_ThrowsArgumentException(string? description)
    {
        var act = () => new PaymentCondition(description!, 1);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("description")
            .WithMessage("*Description is required*");
    }

    [Fact]
    public void Constructor_WhenNumberOfInstallmentsZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new PaymentCondition("À vista", 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numberOfInstallments")
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void Constructor_WhenNumberOfInstallmentsNegative_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new PaymentCondition("30 dias", -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numberOfInstallments");
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsPropertiesAndTrimsDescription()
    {
        var pc = new PaymentCondition("  30/60 dias  ", 2);

        pc.Description.Should().Be("30/60 dias");
        pc.NumberOfInstallments.Should().Be(2);
        pc.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
