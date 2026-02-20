namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Representa uma condição de pagamento (ex.: À vista, 30/60/90) com número fixo de parcelas.
/// </summary>
public class PaymentCondition
{
    public int Id { get; private set; }
    public string Description { get; private set; } = default!;
    public int NumberOfInstallments { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Apenas para EF Core. Não usar em código de domínio.
    /// </summary>
    protected PaymentCondition()
    {
    }

    public PaymentCondition(string description, int numberOfInstallments)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (numberOfInstallments <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numberOfInstallments),
                "Number of installments must be greater than zero.");
        }

        // Id gerado pelo banco (SERIAL/IDENTITY)
        Description = description.Trim();
        NumberOfInstallments = numberOfInstallments;
        CreatedAtUtc = DateTime.UtcNow;
    }
}

