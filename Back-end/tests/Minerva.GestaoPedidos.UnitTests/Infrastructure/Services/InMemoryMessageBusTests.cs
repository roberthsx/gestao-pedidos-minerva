using FluentAssertions;
using Minerva.GestaoPedidos.Tests.Fakes;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Services;

/// <summary>
/// Testes de comportamento: publicar mensagem e verificar que o assinante a recebe.
/// </summary>
public class InMemoryMessageBusTests
{
    [Fact]
    public async Task PublishAsync_AndSubscribe_SubscriberReceivesMessage()
    {
        var bus = new InMemoryMessageBus();
        const string topic = "test-topic";
        const string payload = "hello-world";

        await bus.PublishAsync(topic, payload, CancellationToken.None);

        var received = new List<string>();
        await foreach (var message in bus.SubscribeAsync(topic).WithCancellation(CancellationToken.None))
        {
            received.Add(message);
            break;
        }

        received.Should().ContainSingle().Which.Should().Be(payload);
    }

    [Fact]
    public async Task PublishAsync_MultipleMessages_SubscriberReceivesInOrder()
    {
        var bus = new InMemoryMessageBus();
        const string topic = "multi-topic";

        await bus.PublishAsync(topic, "first", CancellationToken.None);
        await bus.PublishAsync(topic, "second", CancellationToken.None);
        await bus.PublishAsync(topic, "third", CancellationToken.None);

        var received = new List<string>();
        var count = 0;
        await foreach (var message in bus.SubscribeAsync(topic).WithCancellation(CancellationToken.None))
        {
            received.Add(message);
            if (++count >= 3) break;
        }

        received.Should().Equal("first", "second", "third");
    }

    [Fact]
    public async Task SubscribeAsync_DifferentTopics_ReceiveOnlyOwnTopic()
    {
        var bus = new InMemoryMessageBus();
        await bus.PublishAsync("topic-a", "msg-a", CancellationToken.None);
        await bus.PublishAsync("topic-b", "msg-b", CancellationToken.None);

        var receivedA = new List<string>();
        await foreach (var message in bus.SubscribeAsync("topic-a").WithCancellation(CancellationToken.None))
        {
            receivedA.Add(message);
            break;
        }

        receivedA.Should().ContainSingle().Which.Should().Be("msg-a");
    }
}
