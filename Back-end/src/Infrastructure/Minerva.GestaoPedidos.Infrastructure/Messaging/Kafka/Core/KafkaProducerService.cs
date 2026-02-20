using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Constants;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Core;

/// <summary>
/// Kafka producer for domain events with Circuit Breaker e Retry (Polly).
/// Quando o broker está fora, TryProduceAsync retorna false; o handler loga para conciliação.
/// </summary>
public sealed class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly ResiliencePipeline _pipeline;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.Leader,
            MessageTimeoutMs = 5000
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
        _pipeline = BuildResiliencePipeline();
    }

    /// <summary>
    /// Construtor para testes unitários: permite injetar IProducer mockado.
    /// </summary>
    internal KafkaProducerService(IProducer<string, string> producer, ILogger<KafkaProducerService> logger)
    {
        _producer = producer;
        _logger = logger;
        _pipeline = BuildResiliencePipeline();
    }

    private ResiliencePipeline BuildResiliencePipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<KafkaException>(),
                OnRetry = args =>
                {
                    KafkaProducerServiceLogs.LogRetry(_logger, args.Outcome.Exception!, args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder().Handle<KafkaException>(),
                OnOpened = _ =>
                {
                    KafkaProducerServiceLogs.LogCircuitBreakerOpened(_logger);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

    public async Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var sent = await TryProduceAsync(topic, payload, cancellationToken).ConfigureAwait(false);
        if (!sent)
            throw new InvalidOperationException(KafkaMessages.FormatProduceFailed(topic));
    }

    public Task<bool> TryProduceAsync(string topic, string payload, CancellationToken cancellationToken = default) =>
        TryProduceAsync(topic, null, payload, null, cancellationToken);

    public Task<bool> TryProduceAsync(string topic, string payload, IReadOnlyDictionary<string, byte[]>? headers, CancellationToken cancellationToken = default) =>
        TryProduceAsync(topic, null, payload, headers, cancellationToken);

    public async Task<bool> TryProduceAsync(string topic, string? key, string payload, IReadOnlyDictionary<string, byte[]>? headers, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<string, string> { Key = key!, Value = payload };
            if (headers is { Count: > 0 })
            {
                message.Headers = new Headers();
                foreach (var (k, value) in headers)
                    message.Headers.Add(k, value);
            }

            await _pipeline.ExecuteAsync(async ct =>
            {
                var result = await _producer.ProduceAsync(topic, message, ct).ConfigureAwait(false);
                KafkaProducerServiceLogs.LogProduced(_logger, result.Topic, result.Partition.Value, key ?? "(null)");
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            KafkaProducerServiceLogs.LogPublishFailure(_logger, ex, topic);
            return false;
        }
    }

    public void Dispose() => _producer.Dispose();
}
