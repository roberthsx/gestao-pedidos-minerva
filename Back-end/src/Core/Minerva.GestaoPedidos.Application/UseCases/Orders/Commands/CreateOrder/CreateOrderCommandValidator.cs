using FluentValidation;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("O cliente é obrigatório.");

        RuleFor(x => x.PaymentConditionId)
            .NotEmpty()
            .WithMessage("A condição de pagamento é obrigatória.");

        RuleFor(x => x.Items)
            .NotNull()
            .Must(items => items is { Count: > 0 })
            .WithMessage("O pedido deve conter pelo menos um item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductName)
                .NotEmpty()
                .WithMessage("O nome do produto é obrigatório.")
                .MaximumLength(150);

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("A quantidade deve ser maior que zero.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0)
                .WithMessage("O preço unitário deve ser positivo.");
        });
    }
}