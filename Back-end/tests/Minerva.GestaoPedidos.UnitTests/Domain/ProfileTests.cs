using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Testes de guards do construtor de Profile: code e name nulos/vazios/whitespace.
/// Cobre as linhas que lançam ArgumentException (Code is required. / Name is required.).
/// </summary>
public class ProfileTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCodeNullOrWhiteSpace_ThrowsArgumentException(string? code)
    {
        var act = () => new Profile(code!, "Admin");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("code")
            .WithMessage("Code is required.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameNullOrWhiteSpace_ThrowsArgumentException(string? name)
    {
        var act = () => new Profile("ADMIN", name!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name")
            .WithMessage("Name is required.*");
    }

    [Fact]
    public void Constructor_WithValidArgs_TrimsAndUppercasesCode_TrimsName()
    {
        var profile = new Profile("  admin  ", "  Gestão  ");

        profile.Code.Should().Be("ADMIN");
        profile.Name.Should().Be("Gestão");
    }
}
