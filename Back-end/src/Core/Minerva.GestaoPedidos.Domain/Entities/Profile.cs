namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Perfil de acesso do usuário: Admin, Gestão ou Analista.
/// </summary>
public class Profile
{
    public int Id { get; private set; }
    /// <summary>Código usado no JWT e em [Authorize(Roles = "...")]: ADMIN, GESTAO, ANALISTA.</summary>
    public string Code { get; private set; } = default!;
    /// <summary>Nome de exibição: Admin, Gestão, Analista.</summary>
    public string Name { get; private set; } = default!;

    protected Profile() { }

    public Profile(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        // Id gerado pelo banco (SERIAL/IDENTITY)
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}
