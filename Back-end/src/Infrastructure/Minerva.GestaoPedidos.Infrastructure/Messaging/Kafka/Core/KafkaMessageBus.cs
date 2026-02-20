using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Contracts;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Core;

/// <summary>
/// Implementação de IMessageBus usando Kafka (produção). Publica e assina tópicos via Confluent.Kafka.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class KafkaMessageBus : IMessageBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaMessageBus> _logger;
    private readonly string _bootstrapServers;

    public KafkaMessageBus(IConfiguration configuration, ILogger<KafkaMessageBus> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers is required for KafkaMessageBus.");
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.Leader,
            MessageTimeoutMs = 5000
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await _producer.ProduceAsync(topic, new Message<string, string> { Value = payload }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Falha ao publicar no tópico {Topic}", topic);
            throw;
        }
    }

    public async IAsyncEnumerable<string> SubscribeAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = $"minerva-messagebus-{topic}-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var consumerTask = Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            try
            {
                consumer.Subscribe(topic);
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(5));
                        if (result is not null && !string.IsNullOrEmpty(result.Message.Value))
                        {
                            await channel.Writer.WriteAsync(result.Message.Value, cts.Token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao consumir do tópico {Topic}", topic);
                    }
                }
            }
            finally
            {
                consumer.Close();
                channel.Writer.Complete();
            }
        }, cts.Token);

        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    public void Dispose() => _producer.Dispose();
}
