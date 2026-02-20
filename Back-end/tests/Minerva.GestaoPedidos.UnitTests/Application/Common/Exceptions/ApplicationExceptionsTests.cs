using FluentAssertions;
using FluentValidation.Results;
using Minerva.GestaoPedidos.Application.Common.Exceptions;

namespace Minerva.GestaoPedidos.UnitTests.Application.Common.Exceptions;

/// <summary>
/// Testes unitários para exceções da camada Application (garantir instanciação e mensagens/códigos).
/// </summary>
public class ApplicationExceptionsTests
{
    [Fact]
    public void BusinessException_Default_ShouldInstantiate()
    {
        var ex = new BusinessException();

        ex.Should().NotBeNull();
        ex.Should().BeOfType<BusinessException>();
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void BadRequestException_WithMessage_ShouldSetMessageAndInnerNull()
    {
        const string message = "Invalid request.";
        var ex = new BadRequestException(message);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void BadRequestException_WithMessageAndInner_ShouldSetBoth()
    {
        const string message = "Invalid request.";
        var inner = new InvalidOperationException("Inner");
        var ex = new BadRequestException(message, inner);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ConflictException_Default_ShouldUseDefaultMessage()
    {
        var ex = new ConflictException();

        ex.Message.Should().Contain("conflict");
        ex.Should().BeAssignableTo<BusinessException>();
    }

    [Fact]
    public void ConflictException_WithMessage_ShouldSetMessage()
    {
        const string message = "Duplicate email.";
        var ex = new ConflictException(message);

        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ConflictException_WithMessageAndInner_ShouldSetBoth()
    {
        const string message = "Conflict.";
        var inner = new Exception("Inner");
        var ex = new ConflictException(message, inner);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void OrderAlreadyExistsException_ShouldSetExistingOrderIdAndFixedMessage()
    {
        const int existingId = 42;
        var ex = new OrderAlreadyExistsException(existingId);

        ex.ExistingOrderId.Should().Be(existingId);
        ex.Message.Should().Contain("já foi processado");
    }

    [Fact]
    public void ValidationException_Default_ShouldHaveEmptyErrors()
    {
        var ex = new ValidationException();

        ex.Message.Should().Contain("validation");
        ex.Errors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ValidationException_WithFailures_ShouldGroupByPropertyName()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required."),
            new("Email", "Email format invalid."),
            new("Name", "Name is required.")
        };
        var ex = new ValidationException(failures);

        ex.Errors.Should().ContainKey("Email").WhoseValue.Should().HaveCount(2);
        ex.Errors.Should().ContainKey("Name").WhoseValue.Should().ContainSingle().Which.Should().Be("Name is required.");
    }

    [Fact]
    public void NotFoundException_Default_ShouldUseDefaultMessage()
    {
        var ex = new NotFoundException();

        ex.Message.Should().Contain("not found");
    }

    [Fact]
    public void NotFoundException_WithMessage_ShouldSetMessage()
    {
        const string message = "Order '123' was not found.";
        var ex = new NotFoundException(message);

        ex.Message.Should().Be(message);
    }

    [Fact]
    public void NotFoundException_WithMessageAndInner_ShouldSetBoth()
    {
        const string message = "Resource not found.";
        var inner = new KeyNotFoundException();
        var ex = new NotFoundException(message, inner);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ServiceUnavailableException_Default_ShouldUseDefaultMessage()
    {
        var ex = new ServiceUnavailableException();

        ex.Message.Should().Contain("indisponível");
    }

    [Fact]
    public void ServiceUnavailableException_WithMessage_ShouldSetMessage()
    {
        const string message = "Database is down.";
        var ex = new ServiceUnavailableException(message);

        ex.Message.Should().Be(message);
    }

    [Fact]
    public void InfrastructureException_Default_ShouldUseDefaultMessage()
    {
        var ex = new InfrastructureException();

        ex.Message.Should().Be(InfrastructureException.DefaultMessage);
        ex.Should().BeAssignableTo<ServiceUnavailableException>();
    }

    [Fact]
    public void InfrastructureException_WithMessage_ShouldSetMessage()
    {
        const string message = "Database connection timeout.";
        var ex = new InfrastructureException(message);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeNull();
        ex.Should().BeAssignableTo<ServiceUnavailableException>();
    }

    [Fact]
    public void InfrastructureException_WithMessageAndInner_ShouldSetBoth()
    {
        const string message = "Connection failed.";
        var inner = new TimeoutException();
        var ex = new InfrastructureException(message, inner);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().BeSameAs(inner);
    }
}