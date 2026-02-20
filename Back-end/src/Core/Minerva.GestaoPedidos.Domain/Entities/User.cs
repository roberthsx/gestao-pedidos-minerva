using System.Text.RegularExpressions;

namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Entidade de domínio rica para usuário. Regras de integridade aplicadas no construtor e nos métodos de comportamento.
/// </summary>
public partial class User
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public int Id { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public bool Active { get; private set; }
    public int? ProfileId { get; private set; }
    public Profile? Profile { get; private set; }
    /// <summary>Número de registro para login (opcional; quando preenchido permite autenticação por registro/senha).</summary>
    public string? RegistrationNumber { get; private set; }
    /// <summary>Hash da senha (BCrypt) para autenticação.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// Apenas para EF Core. Não usar em código de domínio.
    /// </summary>
    protected User()
    {
    }

    /// <summary>
    /// Construtor rico: valida invariantes e cria um novo usuário.
    /// </summary>
    public User(string firstName, string lastName, string email, bool active)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));

        // Id gerado pelo banco (SERIAL/IDENTITY)
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Active = active;
    }

    public void Activate() => Active = true;
    public void Deactivate() => Active = false;

    public void UpdateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }
}
