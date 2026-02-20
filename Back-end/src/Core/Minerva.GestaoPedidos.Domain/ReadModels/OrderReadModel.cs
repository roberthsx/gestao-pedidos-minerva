namespace Minerva.GestaoPedidos.Domain.ReadModels;

/// <summary>
/// Read model desnormalizado para pedidos (independente de persistência; V2 pode adicionar store de leitura dedicado).
/// Agrega dados de pedido, cliente e prazos de entrega.
/// </summary>
public class OrderReadModel
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = default!;
    public int PaymentConditionId { get; set; }
    public string PaymentConditionDescription { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = default!;
    public bool RequiresManualApproval { get; set; }
    public int DeliveryDays { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<OrderItemReadModel> Items { get; set; } = new();
}