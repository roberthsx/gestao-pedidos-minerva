using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.Application.UseCases.Users.Queries.GetUsers;

public sealed record GetUsersQuery() : IRequest<IReadOnlyList<UserDto>>;

