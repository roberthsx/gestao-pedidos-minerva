namespace Minerva.GestaoPedidos.Application.Contracts;

/// <summary>
/// Port para publicação e assinatura de mensagens por tópico.
/// Implementações reais (Kafka) ficam na Infrastructure; Fakes (in-memory) nos projetos de teste.
/// </summary>
public interface IMessageBus
{
    Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
}