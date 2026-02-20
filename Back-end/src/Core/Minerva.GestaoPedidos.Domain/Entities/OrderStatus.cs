namespace Minerva.GestaoPedidos.Domain.Entities;

/// <summary>
/// Status do ciclo de vida do pedido. Persistido como string no banco (EF HasConversion&lt;string&gt;).
/// </summary>
public enum OrderStatus
{
    Pendente = 1,
    Criado = 2,
    Pago = 3,
    Cancelado = 4
}

