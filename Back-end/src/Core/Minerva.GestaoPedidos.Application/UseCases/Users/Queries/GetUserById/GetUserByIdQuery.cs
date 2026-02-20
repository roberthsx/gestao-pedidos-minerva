using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.UseCases.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(int Id) : IRequest<UserDto?>;

