using FluentAssertions;
using Minerva.GestaoPedidos.Application.UseCases.Users.Commands.CreateUser;

namespace Minerva.GestaoPedidos.UnitTests.Application.UseCases.Users.Commands.CreateUser;

public class CreateUserCommandValidatorTests
{
    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        // Arrange
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: string.Empty,
            Active: true);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Email));
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        // Arrange
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand(
            FirstName: string.Empty,
            LastName: "Doe",
            Email: "john.doe@example.com",
            Active: true);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.FirstName));
    }

    [Fact]
    public void Should_Be_Valid_When_Data_Is_Correct()
    {
        // Arrange
        var validator = new CreateUserCommandValidator();
        var command = new CreateUserCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            Active: true);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
