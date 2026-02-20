using FluentValidation;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.Validators;

/// <summary>
/// Validação de presença dos campos de login. O AuthService fica responsável apenas pela lógica de autenticação.
/// </summary>
public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .WithMessage("A matrícula é obrigatória.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("A senha é obrigatória.");
    }
}