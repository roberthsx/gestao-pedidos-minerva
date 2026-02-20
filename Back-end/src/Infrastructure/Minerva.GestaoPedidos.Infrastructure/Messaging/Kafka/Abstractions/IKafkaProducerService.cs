namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;

/// <summary>
/// Sends domain event payloads to Kafka topics.
/// TryProduceAsync retorna false quando o broker está fora ou circuit breaker aberto; o handler loga para conciliação.
/// </summary>
public interface IKafkaProducerService
{
    /// <summary>
    /// Produces a message to the specified topic. Throws on failure.
    /// </summary>
    Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta enviar a mensagem ao Kafka. Retorna true se enviado; false se broker indisponível ou circuit breaker aberto.
    /// </summary>
    Task<bool> TryProduceAsync(string topic, string payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta enviar a mensagem ao Kafka com headers (ex.: X-Correlation-ID, X-Causation-ID para rastreamento).
    /// </summary>
    Task<bool> TryProduceAsync(string topic, string payload, IReadOnlyDictionary<string, byte[]>? headers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta enviar a mensagem ao Kafka com key (ex.: OrderId para idempotência e particionamento), payload e headers opcionais.
    /// </summary>
    Task<bool> TryProduceAsync(string topic, string? key, string payload, IReadOnlyDictionary<string, byte[]>? headers, CancellationToken cancellationToken = default);
}
