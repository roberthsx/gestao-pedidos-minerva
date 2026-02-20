namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.ExternalModels;

/// <summary>
/// Modelo do payload enviado ao tópico order-approved (Kafka). Formato de contrato externo;
/// alterações em APIs/consumidores devem impactar apenas este tipo e o mapper.
/// </summary>
public sealed class KafkaOrderApprovedPayload
{
    public int OrderId { get; set; }
    public string Status { get; set; } = default!;
    public DateTime ApprovedAtUtc { get; set; }
}
