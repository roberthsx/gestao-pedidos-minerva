using FluentValidation;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.ApproveOrder;

public class ApproveOrderCommandValidator : AbstractValidator<ApproveOrderCommand>
{
    public ApproveOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();
    }
}