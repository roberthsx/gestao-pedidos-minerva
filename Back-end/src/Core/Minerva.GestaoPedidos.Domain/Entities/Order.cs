namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Raiz de agregação para pedidos. Encapsula invariantes e regras de ciclo de vida.
/// </summary>
public class Order
{
    private readonly List<OrderItem> _items = new();

    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public int PaymentConditionId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public bool RequiresManualApproval { get; private set; }
    public DateTime CreatedAt { get; private set; }
    /// <summary>Chave de idempotência (hash de negócio). UNIQUE no banco para bloquear duplicatas por clique duplo ou race.</summary>
    public string? IdempotencyKey { get; private set; }
    /// <summary>Matrícula do usuário que aprovou o pedido (auditoria).</summary>
    public string? ApprovedBy { get; private set; }
    /// <summary>Data/hora UTC da aprovação (auditoria).</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>Define a chave de idempotência antes de persistir. Chamado pela aplicação após criar o pedido.</summary>
    public void SetIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A chave de idempotência é obrigatória.", nameof(key));
        IdempotencyKey = key;
    }

    public Customer? Customer { get; private set; }
    public PaymentCondition? PaymentCondition { get; private set; }
    public DeliveryTerm? DeliveryTerm { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Apenas para EF Core. Não usar em código de domínio.
    /// </summary>
    protected Order()
    {
    }

    private Order(int customerId, int paymentConditionId, DateTime orderDate)
    {
        if (customerId <= 0)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        if (paymentConditionId <= 0)
        {
            throw new ArgumentException("Payment condition id is required.", nameof(paymentConditionId));
        }

        if (orderDate == default)
        {
            throw new ArgumentException("Order date is required.", nameof(orderDate));
        }

        // Id gerado pelo banco (SERIAL/IDENTITY)
        CustomerId = customerId;
        PaymentConditionId = paymentConditionId;
        OrderDate = orderDate;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Método fábrica que cria um pedido com seus itens e aplica a regra de ouro
    /// para TotalAmount e status de aprovação.
    /// </summary>
    public static Order Create(
        int customerId,
        int paymentConditionId,
        DateTime? orderDate,
        IEnumerable<(string ProductName, int Quantity, decimal UnitPrice)> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var materializedItems = items.ToList();
        if (materializedItems.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.", nameof(items));
        }

        var effectiveDate = orderDate ?? DateTime.UtcNow;
        var order = new Order(customerId, paymentConditionId, effectiveDate);

        foreach (var item in materializedItems)
        {
            order.AddOrderItem(item.ProductName, item.Quantity, item.UnitPrice);
        }

        order.ApplyGoldenRule();

        return order;
    }

    /// <summary>
    /// Adiciona um item ao pedido garantindo invariantes e recalculando o TotalAmount.
    /// </summary>
    public OrderItem AddOrderItem(string productName, int quantity, decimal unitPrice)
    {
        var item = new OrderItem(Id, productName, quantity, unitPrice);
        _items.Add(item);
        TotalAmount += item.TotalPrice;
        return item;
    }

    private void ApplyGoldenRule()
    {
        // Invariante: TotalAmount = soma dos TotalPrice dos itens
        TotalAmount = _items.Sum(i => i.TotalPrice);
        if (TotalAmount > 5000m)
        {
            Status = OrderStatus.Criado;
            RequiresManualApproval = true;
        }
        else
        {
            Status = OrderStatus.Pago;
            RequiresManualApproval = false;
        }
    }

    /// <summary>
    /// Aprova o pedido e registra auditoria (matrícula e data/hora).
    /// </summary>
    /// <param name="approvedBy">Matrícula do usuário que aprovou (ex.: do claim NameIdentifier).</param>
    public void Approve(string? approvedBy)
    {
        if (Status == OrderStatus.Pago)
        {
            throw new InvalidOperationException("Order is already paid.");
        }

        if (Status == OrderStatus.Cancelado)
        {
            throw new InvalidOperationException("Cannot approve a canceled order.");
        }

        Status = OrderStatus.Pago;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Pago)
        {
            throw new InvalidOperationException("Cannot cancel a paid order.");
        }

        Status = OrderStatus.Cancelado;
    }
}

