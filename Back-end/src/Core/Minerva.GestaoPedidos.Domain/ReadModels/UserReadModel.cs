namespace Minerva.GestaoPedidos.Domain.ReadModels;

/// <summary>
/// Read model desnormalizado para usuário (independente de persistência; V2 pode adicionar store de leitura dedicado).
/// Este modelo é intencionalmente independente de persistência (sem atributos do EF Core).
/// </summary>
public class UserReadModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool Active { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}