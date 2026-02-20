namespace Minerva.GestaoPedidos.Domain.ReadModels;

/// <summary>
/// Read model para item de pedido (linha do pedido). Independente de persistência.
/// </summary>
public class OrderItemReadModel
{
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}