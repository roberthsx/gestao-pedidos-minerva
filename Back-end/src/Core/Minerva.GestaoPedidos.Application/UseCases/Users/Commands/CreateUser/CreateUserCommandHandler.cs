using AutoMapper;
using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Events;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Application.UseCases.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDomainService _userDomainService;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUserDomainService userDomainService,
        IPublisher publisher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _userDomainService = userDomainService;
        _publisher = publisher;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await _userDomainService.ValidateUniqueEmailAsync(request.Email, cancellationToken);

        var user = new User(request.FirstName, request.LastName, request.Email, request.Active);
        var created = await _userRepository.AddAsync(user, cancellationToken);

        var @event = new UserCreatedEvent(
            created.Id,
            created.FirstName,
            created.LastName,
            created.Email,
            created.Active,
            DateTime.UtcNow);

        await _publisher.Publish(@event, cancellationToken);

        return _mapper.Map<UserDto>(created);
    }
}

