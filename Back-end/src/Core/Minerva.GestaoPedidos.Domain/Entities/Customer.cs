using System.Text.RegularExpressions;

namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Entidade de domínio rica para cliente. Garante invariantes de nome e e-mail.
/// </summary>
public partial class Customer
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Apenas para EF Core. Não usar em código de domínio.
    /// </summary>
    protected Customer()
    {
    }

    public Customer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (!EmailRegex().IsMatch(email))
        {
            throw new ArgumentException("Email format is invalid.", nameof(email));
        }

        // Id gerado pelo banco (SERIAL/IDENTITY)
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        CreatedAtUtc = DateTime.UtcNow;
    }
}

