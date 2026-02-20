using AutoMapper;
using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Application.UseCases.Users.Queries.GetUsers;

/// <summary>
/// Handler de consulta do lado de leitura apoiado em IUserReadRepository (mesmo que GetUserById).
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserReadRepository _userReadRepository;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUserReadRepository userReadRepository, IMapper mapper)
    {
        _userReadRepository = userReadRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userReadRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<List<UserDto>>(users);
    }
}