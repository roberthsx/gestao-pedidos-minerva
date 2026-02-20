using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Mappers;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Publishers;

/// <summary>
/// Publica Order no tópico order-created (Confluent.Kafka). Converte a entidade de domínio para o payload
/// externo (ACL) via mapper; Application nunca vê o contrato Kafka.
/// </summary>
public sealed class OrderCreatedKafkaPublisher : IOrderCreatedPublisher
{
    public const string OrderCreatedTopic = "order-created";
    public const string HeaderCorrelationId = "X-Correlation-ID";
    public const string HeaderCausationId = "X-Causation-ID";

    private readonly IKafkaProducerService _producer;
    private readonly ILogger<OrderCreatedKafkaPublisher> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public OrderCreatedKafkaPublisher(IKafkaProducerService producer, ILogger<OrderCreatedKafkaPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public Task<bool> PublishOrderCreatedAsync(Order order, string? correlationId, string? causationId, CancellationToken cancellationToken = default)
    {
        if (order is null || order.Id <= 0)
            return Task.FromResult(false);

        var payloadModel = OrderToKafkaOrderCreatedPayloadMapper.Map(order);
        var payload = JsonSerializer.Serialize(payloadModel, JsonOptions);

        IReadOnlyDictionary<string, byte[]>? headers = null;
        if (!string.IsNullOrWhiteSpace(correlationId) || !string.IsNullOrWhiteSpace(causationId))
        {
            var dict = new Dictionary<string, byte[]>();
            if (!string.IsNullOrWhiteSpace(correlationId))
                dict[HeaderCorrelationId] = Encoding.UTF8.GetBytes(correlationId);
            if (!string.IsNullOrWhiteSpace(causationId))
                dict[HeaderCausationId] = Encoding.UTF8.GetBytes(causationId);
            headers = dict;
        }

        return _producer.TryProduceAsync(OrderCreatedTopic, order.Id.ToString(), payload, headers, cancellationToken);
    }
}
