namespace Minerva.GestaoPedidos.Domain.ReadModels;

/// <summary>
/// Read model para lookup de clientes (listagem para seleção). Independente de persistência.
/// </summary>
public class CustomerReadModel
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}