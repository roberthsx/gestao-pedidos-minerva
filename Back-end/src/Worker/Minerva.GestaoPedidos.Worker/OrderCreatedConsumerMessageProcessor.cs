using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.ExternalModels;
using Polly;
using System.Text;
using System.Text.Json;

namespace Minerva.GestaoPedidos.Worker;

/// <summary>
/// Processa uma mensagem do tópico order-created: desserializa, chama o handler e envia para DLQ em falha.
/// </summary>
internal sealed class OrderCreatedConsumerMessageProcessor
{
    private const int MaxRetries = 3;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public OrderCreatedConsumerMessageProcessor(IServiceProvider serviceProvider, ILogger<OrderCreatedKafkaConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessOneMessageAsync(IConsumer<Ignore, string> consumer, ConsumeResult<Ignore, string> result, ResiliencePipeline retryPolicy, CancellationToken stoppingToken)
    {
        KafkaOrderCreatedPayload? message;
        try
        {
            message = JsonSerializer.Deserialize<KafkaOrderCreatedPayload>(result.Message.Value, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payload inválido (offset {Offset}). Enviando para DLQ.", result.TopicPartitionOffset.Offset);
            await SendToDlqAsync(result.Message.Value, result.TopicPartitionOffset, stoppingToken).ConfigureAwait(false);
            consumer.Commit(result);
            return;
        }
        var orderId = message?.OrderId ?? 0;
        _logger.LogInformation("Mensagem recebida. OrderId={OrderId}, Offset={Offset}.", orderId, result.TopicPartitionOffset.Offset);
        var correlationId = GetHeaderValue(result.Message.Headers, OrderCreatedKafkaConsumerHostedService.HeaderCorrelationId) ?? Guid.NewGuid().ToString("N");
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            try
            {
                await retryPolicy.ExecuteAsync(async ct =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IOrderCreatedMessageHandler>();
                    await handler.ProcessAsync(orderId, correlationId, ct).ConfigureAwait(false);
                }, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processamento falhou após {Max} tentativas (offset {Offset}). Enviando para DLQ.", MaxRetries, result.TopicPartitionOffset.Offset);
                await SendToDlqAsync(result.Message.Value, result.TopicPartitionOffset, stoppingToken).ConfigureAwait(false);
            }
        }
        consumer.Commit(result);
    }

    private async Task SendToDlqAsync(string originalPayload, TopicPartitionOffset offset, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducerService>();
            var sent = await producer.TryProduceAsync(OrderCreatedKafkaConsumerHostedService.OrderCreatedDlqTopic, originalPayload, cancellationToken).ConfigureAwait(false);
            if (sent)
                _logger.LogWarning("Mensagem enviada para DLQ. Offset {Offset}.", offset.Offset);
            else
                _logger.LogError("Falha ao enviar para DLQ. Offset {Offset}.", offset.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar para DLQ (offset {Offset}).", offset.Offset);
        }
    }

    private static string? GetHeaderValue(Headers? headers, string key)
    {
        if (headers is null) return null;
        foreach (var h in headers)
        {
            if (!string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
            var bytes = h.GetValueBytes();
            if (bytes is null || bytes.Length == 0) return null;
            return Encoding.UTF8.GetString(bytes);
        }
        return null;
    }
}
