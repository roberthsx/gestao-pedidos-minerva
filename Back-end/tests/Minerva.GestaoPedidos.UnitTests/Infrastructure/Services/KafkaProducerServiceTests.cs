using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Core;
using Moq;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Services;

/// <summary>
/// Testes unitários do KafkaProducerService: sucesso da publicação, falha (retorno false) e ProduceAsync que lança quando TryProduceAsync falha.
/// Usa o construtor internal que recebe IProducer para injetar mock.
/// </summary>
public class KafkaProducerServiceTests
{
    private static DeliveryResult<string, string> CreateDeliveryResult(string topic = "test-topic")
    {
        return new DeliveryResult<string, string>
        {
            Topic = topic,
            Partition = new Partition(0),
            Offset = new Offset(0)
        };
    }

    [Fact]
    public async Task TryProduceAsync_WhenProducerSucceeds_ReturnsTrue()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeliveryResult());

        var logger = new Mock<ILogger<KafkaProducerService>>();
        using var sut = new KafkaProducerService(mockProducer.Object, logger.Object);

        var result = await sut.TryProduceAsync("order-created", "payload", CancellationToken.None);

        result.Should().BeTrue();
        mockProducer.Verify(
            x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryProduceAsync_WhenProducerThrowsKafkaException_ReturnsFalse()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KafkaException(ErrorCode.Local_Transport));

        var logger = new Mock<ILogger<KafkaProducerService>>();
        using var sut = new KafkaProducerService(mockProducer.Object, logger.Object);

        var result = await sut.TryProduceAsync("order-created", "payload", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryProduceAsync_WhenProducerThrowsGenericException_ReturnsFalse()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));

        var logger = new Mock<ILogger<KafkaProducerService>>();
        using var sut = new KafkaProducerService(mockProducer.Object, logger.Object);

        var result = await sut.TryProduceAsync("order-approved", "payload", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryProduceAsync_WithKeyAndHeaders_ForwardsToProducer()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        Message<string, string>? capturedMessage = null;
        string? capturedTopic = null;
        mockProducer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, msg, _) => { capturedTopic = topic; capturedMessage = msg; })
            .ReturnsAsync(CreateDeliveryResult());

        var logger = new Mock<ILogger<KafkaProducerService>>();
        using var sut = new KafkaProducerService(mockProducer.Object, logger.Object);
        var headers = new Dictionary<string, byte[]> { ["X-Correlation-ID"] = System.Text.Encoding.UTF8.GetBytes("corr-1") };

        var result = await sut.TryProduceAsync("order-created", "key-1", "{\"orderId\":1}", headers, CancellationToken.None);

        result.Should().BeTrue();
        capturedTopic.Should().Be("order-created");
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Key.Should().Be("key-1");
        capturedMessage.Value.Should().Be("{\"orderId\":1}");
        capturedMessage.Headers.Should().NotBeNull();
        capturedMessage.Headers!.Count.Should().Be(1);
    }

    [Fact]
    public async Task ProduceAsync_WhenTryProduceAsyncReturnsTrue_DoesNotThrow()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeliveryResult());

        var logger = new Mock<ILogger<KafkaProducerService>>();
        using var sut = new KafkaProducerService(mockProducer.Object, logger.Object);

        var act = async () => await sut.ProduceAsync("topic", "payload", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProduceAsync_WhenTryProduceAsyncReturnsFalse_ThrowsInvalidOperationException()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KafkaException(ErrorCode.Local_Transport));

        var logger = new Mock<ILogger<KafkaProducerService>>();
        using var sut = new KafkaProducerService(mockProducer.Object, logger.Object);

        var act = async () => await sut.ProduceAsync("order-created", "payload", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Falha ao publicar no tópico order-created*");
    }

    [Fact]
    public void Dispose_DisposesProducer()
    {
        var mockProducer = new Mock<IProducer<string, string>>();
        mockProducer.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeliveryResult());
        var logger = new Mock<ILogger<KafkaProducerService>>();

        using (var sut = new KafkaProducerService(mockProducer.Object, logger.Object))
        {
        }

        mockProducer.Verify(x => x.Dispose(), Times.Once);
    }
}
