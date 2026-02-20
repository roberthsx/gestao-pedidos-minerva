using AutoMapper;
using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Application.UseCases.Users.Queries.GetUserById;

/// <summary>
/// Handler de consulta do lado de leitura apoiado em IUserReadRepository.
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserReadRepository _userReadRepository;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IUserReadRepository userReadRepository, IMapper mapper)
    {
        _userReadRepository = userReadRepository;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userReadRepository.GetByIdAsync(request.Id, cancellationToken);
        return user is null ? null : _mapper.Map<UserDto>(user);
    }
}

