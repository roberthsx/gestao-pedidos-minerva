using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Testes de borda para a entidade User: validações de nome, email (Regex) e transições.
/// </summary>
public class UserTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesUser()
    {
        var user = new User("John", "Doe", "john@example.com", true);

        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("john@example.com");
        user.Active.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenFirstNameNull_ThrowsArgumentException()
    {
        var act = () => new User(null!, "Doe", "j@j.com", true);

        act.Should().Throw<ArgumentException>().WithParameterName("firstName");
    }

    [Fact]
    public void Constructor_WhenFirstNameWhiteSpace_ThrowsArgumentException()
    {
        var act = () => new User("   ", "Doe", "j@j.com", true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhenLastNameNull_ThrowsArgumentException()
    {
        var act = () => new User("John", null!, "j@j.com", true);

        act.Should().Throw<ArgumentException>().WithParameterName("lastName");
    }

    [Fact]
    public void Constructor_WhenEmailNull_ThrowsArgumentException()
    {
        var act = () => new User("John", "Doe", null!, true);

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void Constructor_WhenEmailInvalidFormat_ThrowsArgumentException()
    {
        var act = () => new User("John", "Doe", "not-an-email", true);

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void Constructor_WhenEmailNoAt_ThrowsArgumentException()
    {
        var act = () => new User("John", "Doe", "bad.com", true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TrimsAndLowercasesEmail()
    {
        var user = new User("John", "Doe", "JOHN@EXAMPLE.COM", true);

        user.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Activate_SetsActiveTrue()
    {
        var user = new User("J", "D", "j@d.com", false);
        user.Activate();
        user.Active.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsActiveFalse()
    {
        var user = new User("J", "D", "j@d.com", true);
        user.Deactivate();
        user.Active.Should().BeFalse();
    }

    [Fact]
    public void UpdateName_ValidArgs_UpdatesTrimmed()
    {
        var user = new User("A", "B", "a@b.com", true);
        user.UpdateName("  Jane  ", "  Smith  ");
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Fact]
    public void UpdateName_WhenFirstNameNull_ThrowsArgumentException()
    {
        var user = new User("J", "D", "j@d.com", true);
        var act = () => user.UpdateName(null!, "D");

        act.Should().Throw<ArgumentException>().WithParameterName("firstName");
    }

    [Fact]
    public void UpdateName_WhenLastNameNull_ThrowsArgumentException()
    {
        var user = new User("J", "D", "j@d.com", true);
        var act = () => user.UpdateName("J", null!);

        act.Should().Throw<ArgumentException>().WithParameterName("lastName");
    }
}
