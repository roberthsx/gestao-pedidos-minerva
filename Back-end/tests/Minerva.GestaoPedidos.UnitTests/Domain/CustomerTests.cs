using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Testes de borda para a entidade Customer: validações de nome e email (Regex).
/// </summary>
public class CustomerTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesCustomer()
    {
        var customer = new Customer("Minerva Foods", "contato@minerva.com");

        customer.Name.Should().Be("Minerva Foods");
        customer.Email.Should().Be("contato@minerva.com");
        customer.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WhenNameNull_ThrowsArgumentException()
    {
        var act = () => new Customer(null!, "a@a.com");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_WhenNameWhiteSpace_ThrowsArgumentException()
    {
        var act = () => new Customer("   ", "a@a.com");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhenEmailNull_ThrowsArgumentException()
    {
        var act = () => new Customer("Name", null!);

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void Constructor_WhenEmailInvalidFormat_ThrowsArgumentException()
    {
        var act = () => new Customer("Name", "invalid");

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void Constructor_WhenEmailNoDomain_ThrowsArgumentException()
    {
        var act = () => new Customer("Name", "user@");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TrimsAndLowercasesEmail()
    {
        var customer = new Customer("C", "USER@DOMAIN.COM");

        customer.Email.Should().Be("user@domain.com");
        customer.Name.Should().Be("C");
    }
}
