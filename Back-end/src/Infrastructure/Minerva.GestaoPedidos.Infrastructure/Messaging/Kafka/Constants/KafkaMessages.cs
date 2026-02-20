using System.Diagnostics.CodeAnalysis;

namespace Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Constants;

/// <summary>
/// Mensagens de log e exceção do Kafka centralizadas em português.
/// Utilizar nas chamadas de log e em InvalidOperationException do producer.
/// </summary>
[ExcludeFromCodeCoverage]
public static class KafkaMessages
{
    /// <summary>Mensagem da exceção quando ProduceAsync falha (broker indisponível ou circuit breaker aberto). Use com string.Format(ProduceFailedFormat, topic).</summary>
    public const string ProduceFailedFormat = "Falha ao publicar no tópico {0}. Broker pode estar indisponível ou circuit breaker aberto.";

    /// <summary>Log de retry: infraestrutura Kafka indisponível, tentativa de reconexão. Placeholder: {Attempt}.</summary>
    public const string RetryMessage = "Infraestrutura indisponível (Kafka). Tentativa de reconexão #{Attempt}.";

    /// <summary>Log quando o circuit breaker abre: publicações falharão até o broker voltar.</summary>
    public const string CircuitBreakerOpenedMessage = "Circuit breaker Kafka aberto. Publicações falharão até o broker voltar.";

    /// <summary>Log de sucesso ao publicar. Placeholders: {Topic}, {Partition}, {Key}.</summary>
    public const string ProducedDebugMessage = "Mensagem publicada em {Topic} na partição {Partition} com chave {Key}.";

    /// <summary>Log de falha ao publicar (catch). Placeholder: {Topic}.</summary>
    public const string PublishFailureMessage = "Infraestrutura indisponível. Falha ao publicar no Kafka (tópico {Topic}).";

    /// <summary>Retorna a mensagem formatada para InvalidOperationException em ProduceAsync.</summary>
    public static string FormatProduceFailed(string topic) =>
        string.Format(ProduceFailedFormat, topic);
}
