using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Core;

/// <summary>
/// Logs do KafkaProducerService via Source Generator (LoggerMessage). Mensagens em português.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class KafkaProducerServiceLogs
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Mensagem publicada em {Topic} na partição {Partition} com chave {Key}.")]
    public static partial void LogProduced(ILogger logger, string topic, int partition, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Infraestrutura indisponível. Falha ao publicar no Kafka (tópico {Topic}).")]
    public static partial void LogPublishFailure(ILogger logger, Exception ex, string topic);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Infraestrutura indisponível (Kafka). Tentativa de reconexão #{Attempt}.")]
    public static partial void LogRetry(ILogger logger, Exception ex, int attempt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Circuit breaker Kafka aberto. Publicações falharão até o broker voltar.")]
    public static partial void LogCircuitBreakerOpened(ILogger logger);
}
