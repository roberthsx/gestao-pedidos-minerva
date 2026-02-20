using Confluent.Kafka;

namespace Minerva.GestaoPedidos.Worker.Resilience;

/// <summary>
/// Cria o builder do consumer Kafka para o tópico order-created.
/// </summary>
internal static class KafkaConsumerBuilderFactory
{
    public static ConsumerBuilder<Ignore, string> Create(string bootstrapServers, string groupId, ILogger logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 15000,
            AllowAutoCreateTopics = true,
            SecurityProtocol = SecurityProtocol.Plaintext,
            SocketKeepaliveEnable = true,
            MetadataMaxAgeMs = 180000,
            ReconnectBackoffMs = 1000,
            ReconnectBackoffMaxMs = 10000
        };
        return new ConsumerBuilder<Ignore, string>(config)
            .SetPartitionsAssignedHandler((_, partitions) => logger.LogWarning("[Kafka] Partições atribuídas: {Partitions}.", string.Join(", ", partitions)))
            .SetErrorHandler((_, e) => logger.LogError("Erro no Kafka: {Reason}", e.Reason));
    }
}
