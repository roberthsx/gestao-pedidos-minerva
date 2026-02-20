using MediatR;

namespace Minerva.GestaoPedidos.Domain.Events;

/// <summary>
/// Evento de domínio disparado após a criação de um usuário no lado de escrita.
/// Usado para sincronizar dados para o lado de leitura.
/// </summary>
public sealed class UserCreatedEvent : INotification
{
    public UserCreatedEvent(int id, string firstName, string lastName, string email, bool active, DateTime createdAtUtc)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Active = active;
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public bool Active { get; }
    public DateTime CreatedAtUtc { get; }
}

