using AutoMapper;
using FluentAssertions;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Application.UseCases.Users.Queries.GetUsers;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Moq;

namespace Minerva.GestaoPedidos.UnitTests.Application.UseCases.Users.Queries.GetUsers;

/// <summary>
/// Testes de fluxo completo: GetUsersQueryHandler chama repositório e retorna lista mapeada.
/// </summary>
public class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_CallsRepositoryAndReturnsMappedList()
    {
        var users = new List<UserReadModel>
        {
            new()
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@test.com",
                Active = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@test.com",
                Active = false,
                CreatedAtUtc = DateTime.UtcNow
            }
        };
        var dtos = new List<UserDto>
        {
            new(1, "John", "Doe", "john@test.com", true),
            new(2, "Jane", "Doe", "jane@test.com", false)
        };

        var repoMock = new Mock<IUserReadRepository>();
        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<List<UserDto>>(users))
            .Returns(dtos);

        var handler = new GetUsersQueryHandler(repoMock.Object, mapperMock.Object);
        var query = new GetUsersQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        repoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        mapperMock.Verify(m => m.Map<List<UserDto>>(users), Times.Once);
        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("John");
        result[1].FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        var repoMock = new Mock<IUserReadRepository>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserReadModel>());

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<UserDto>>(It.IsAny<IReadOnlyList<UserReadModel>>())).Returns(new List<UserDto>());

        var handler = new GetUsersQueryHandler(repoMock.Object, mapperMock.Object);

        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().NotBeNull().And.BeEmpty();
    }
}
