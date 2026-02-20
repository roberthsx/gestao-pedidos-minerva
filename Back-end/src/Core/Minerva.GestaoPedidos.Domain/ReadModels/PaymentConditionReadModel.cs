namespace Minerva.GestaoPedidos.Domain.ReadModels;

/// <summary>
/// Read model para lookup de condições de pagamento (listagem para seleção). Independente de persistência.
/// </summary>
public class PaymentConditionReadModel
{
    public int Id { get; set; }
    public string Description { get; set; } = default!;
    public int NumberOfInstallments { get; set; }
}