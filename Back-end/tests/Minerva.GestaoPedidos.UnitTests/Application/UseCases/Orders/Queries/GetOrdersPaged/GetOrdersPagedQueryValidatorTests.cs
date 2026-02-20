using FluentAssertions;
using FluentValidation.TestHelper;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Queries.GetOrdersPaged;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Application.UseCases.Orders.Queries.GetOrdersPaged;

/// <summary>
/// Testes de fronteira do GetOrdersPagedQueryValidator: PageSize, PageNumber e Status.
/// </summary>
public class GetOrdersPagedQueryValidatorTests
{
    private readonly GetOrdersPagedQueryValidator _validator = new();

    private static GetOrdersPagedQuery Query(
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int pageNumber = 1,
        int pageSize = 20) =>
        new GetOrdersPagedQuery(status, dateFrom, dateTo, pageNumber, pageSize);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSize_ZeroOrNegative_ShouldFail(int pageSize)
    {
        var query = Query(pageSize: pageSize);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("PageSize deve ser maior que zero.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void PageSize_Positive_ShouldPass(int pageSize)
    {
        var query = Query(pageSize: pageSize);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageNumber_LessThanOne_ShouldFail(int pageNumber)
    {
        var query = Query(pageNumber: pageNumber);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorMessage("PageNumber deve ser maior ou igual a 1.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    public void PageNumber_OneOrGreater_ShouldPass(int pageNumber)
    {
        var query = Query(pageNumber: pageNumber);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData("Pendente")]
    [InlineData("Criado")]
    [InlineData("Pago")]
    [InlineData("Cancelado")]
    [InlineData("pendente")]
    [InlineData("PAGO")]
    public void Status_ValidValue_ShouldPass(string status)
    {
        var query = Query(status: status);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("Invalido")]
    [InlineData("X")]
    public void Status_InvalidValue_ShouldFail(string status)
    {
        var query = Query(status: status);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("O status enviado é inválido. Valores permitidos: Pendente, Criado, Pago, Cancelado");
    }

    [Fact]
    public void Status_EmptyOrNull_ShouldNotValidateStatusRule()
    {
        var query = Query(status: null, pageNumber: 1, pageSize: 10);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void ValidQuery_ShouldHaveNoErrors()
    {
        var query = Query(status: "Criado", pageNumber: 1, pageSize: 20);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
