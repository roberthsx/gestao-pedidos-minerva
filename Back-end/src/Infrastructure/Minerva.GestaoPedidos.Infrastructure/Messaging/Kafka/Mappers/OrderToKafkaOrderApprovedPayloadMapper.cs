using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.ExternalModels;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Mappers;

/// <summary>
/// Mapeia a entidade de domínio Order para o payload externo do tópico order-approved (ACL).
/// </summary>
internal static class OrderToKafkaOrderApprovedPayloadMapper
{
    public static KafkaOrderApprovedPayload Map(Order order)
    {
        if (order is null)
            throw new ArgumentNullException(nameof(order));
        return new KafkaOrderApprovedPayload
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            ApprovedAtUtc = DateTime.UtcNow
        };
    }
}
