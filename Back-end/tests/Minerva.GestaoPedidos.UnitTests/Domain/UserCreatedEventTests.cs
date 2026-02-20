using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Events;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Domain;

/// <summary>
/// Garante que as propriedades públicas de UserCreatedEvent refletem os dados passados no construtor.
/// </summary>
public class UserCreatedEventTests
{
    [Fact]
    public void Constructor_WithValidArgs_PropertiesReflectConstructorValues()
    {
        var id = 42;
        var firstName = "Maria";
        var lastName = "Silva";
        var email = "maria.silva@minerva.com";
        var active = true;
        var createdAtUtc = new DateTime(2026, 2, 11, 10, 0, 0, DateTimeKind.Utc);

        var evt = new UserCreatedEvent(id, firstName, lastName, email, active, createdAtUtc);

        evt.Id.Should().Be(id);
        evt.FirstName.Should().Be(firstName);
        evt.LastName.Should().Be(lastName);
        evt.Email.Should().Be(email);
        evt.Active.Should().Be(active);
        evt.CreatedAtUtc.Should().Be(createdAtUtc);
    }

    [Fact]
    public void Constructor_WithActiveFalse_ReflectsInProperty()
    {
        var evt = new UserCreatedEvent(1, "A", "B", "a@b.com", active: false, DateTime.UtcNow);

        evt.Active.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ImplementsINotification()
    {
        var evt = new UserCreatedEvent(1, "A", "B", "a@b.com", true, DateTime.UtcNow);

        evt.Should().BeAssignableTo<MediatR.INotification>();
    }
}
