using System.Text.Json;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Mappers;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Publishers;

/// <summary>
/// Publica Order aprovado no tópico order-approved (Kafka). Converte a entidade de domínio para o payload
/// externo (ACL) via mapper; Application nunca vê o contrato Kafka.
/// </summary>
public sealed class OrderApprovedKafkaPublisher : IOrderApprovedPublisher
{
    public const string OrderApprovedTopic = "order-approved";

    private readonly IKafkaProducerService _producer;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public OrderApprovedKafkaPublisher(IKafkaProducerService producer)
    {
        _producer = producer;
    }

    public Task<bool> PublishOrderApprovedAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order is null || order.Id <= 0)
            return Task.FromResult(false);

        var payloadModel = OrderToKafkaOrderApprovedPayloadMapper.Map(order);
        var payload = JsonSerializer.Serialize(payloadModel, JsonOptions);
        return _producer.TryProduceAsync(OrderApprovedTopic, payload, null, cancellationToken);
    }
}
