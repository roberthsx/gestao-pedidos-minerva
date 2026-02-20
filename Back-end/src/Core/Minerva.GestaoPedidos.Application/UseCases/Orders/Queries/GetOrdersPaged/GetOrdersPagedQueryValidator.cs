using FluentValidation;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Queries.GetOrdersPaged;

public class GetOrdersPagedQueryValidator : AbstractValidator<GetOrdersPagedQuery>
{
    private const string AllowedValues = "Pendente, Criado, Pago, Cancelado";

    public GetOrdersPagedQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize deve ser maior que zero.");

        RuleFor(x => x.Status)
            .Must(BeValidOrderStatusOrEmpty)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("O status enviado é inválido. Valores permitidos: " + AllowedValues);
    }

    private static bool BeValidOrderStatusOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        return Enum.TryParse<OrderStatus>(value.Trim(), ignoreCase: true, out _);
    }
}