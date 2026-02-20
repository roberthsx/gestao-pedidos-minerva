using MediatR;
using Minerva.GestaoPedidos.Application.Common.Attributes;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.UseCases.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    [property: LogSensitive] string Email,
    bool Active
) : IRequest<UserDto>;
