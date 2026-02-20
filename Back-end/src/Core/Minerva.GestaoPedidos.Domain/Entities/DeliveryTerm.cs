namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Condições de entrega associadas a um pedido (ex.: data estimada de entrega e prazo).
/// </summary>
public class DeliveryTerm
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public DateTime EstimatedDeliveryDate { get; private set; }
    public int DeliveryDays { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Apenas para EF Core. Não usar em código de domínio.
    /// </summary>
    protected DeliveryTerm()
    {
    }

    public DeliveryTerm(int orderId, DateTime orderDate, int deliveryDays)
    {
        if (orderId <= 0)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        if (deliveryDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryDays), "Delivery days must be greater than zero.");
        }

        OrderId = orderId;
        DeliveryDays = deliveryDays;
        CreatedAt = DateTime.UtcNow;
        EstimatedDeliveryDate = orderDate.AddDays(deliveryDays);
        // Id gerado pelo banco (SERIAL/IDENTITY)
    }
}

