using AutoMapper;
using FluentAssertions;
using MediatR;
using Moq;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Application.UseCases.Users.Commands.CreateUser;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.UnitTests.Application.UseCases.Users.Commands.CreateUser;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_User_And_Return_Dto()
    {
        // Arrange
        var repositoryMock = new Mock<IUserRepository>();
        var domainServiceMock = new Mock<IUserDomainService>();
        var publisherMock = new Mock<IPublisher>();
        var mapperMock = new Mock<IMapper>();

        domainServiceMock
            .Setup(s => s.ValidateUniqueEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        mapperMock
            .Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns((User u) => new UserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Active));

        var handler = new CreateUserCommandHandler(
            repositoryMock.Object,
            domainServiceMock.Object,
            publisherMock.Object,
            mapperMock.Object);

        var command = new CreateUserCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            Active: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be(command.FirstName);
        result.LastName.Should().Be(command.LastName);
        result.Email.Should().Be(command.Email);
        result.Active.Should().BeTrue();

        domainServiceMock.Verify(
            s => s.ValidateUniqueEmailAsync(command.Email, It.IsAny<CancellationToken>()),
            Times.Once);
        repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
        publisherMock.Verify(
            p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
