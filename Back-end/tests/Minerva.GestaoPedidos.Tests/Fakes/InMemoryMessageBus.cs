using System.Collections.Concurrent;
using System.Threading.Channels;
using Minerva.GestaoPedidos.Application.Contracts;

namespace Minerva.GestaoPedidos.Tests.Fakes;

/// <summary>
/// Fake in-memory message bus para testes (integração e unitários).
/// Não faz parte do código de produção.
/// </summary>
public class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, Channel<string>> _topics = new();

    public Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(topic, _ => Channel.CreateUnbounded<string>());
        return channel.Writer.WriteAsync(payload, cancellationToken).AsTask();
    }

    public async IAsyncEnumerable<string> SubscribeAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(topic, _ => Channel.CreateUnbounded<string>());

        while (!cancellationToken.IsCancellationRequested)
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out var message))
                {
                    yield return message;
                }
            }
        }
    }
}
