using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.ExternalModels;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Mappers;

/// <summary>
/// Mapeia a entidade de domínio Order para o payload externo do tópico order-created (ACL).
/// </summary>
internal static class OrderToKafkaOrderCreatedPayloadMapper
{
    public static KafkaOrderCreatedPayload Map(Order order)
    {
        if (order is null)
            throw new ArgumentNullException(nameof(order));
        return new KafkaOrderCreatedPayload { OrderId = order.Id };
    }
}
