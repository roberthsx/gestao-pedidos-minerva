namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.ExternalModels;

/// <summary>
/// Modelo do payload enviado ao tópico order-created (Kafka). Formato de contrato externo;
/// alterações em APIs/consumidores devem impactar apenas este tipo e o mapper.
/// </summary>
public sealed class KafkaOrderCreatedPayload
{
    public int OrderId { get; set; }
}
