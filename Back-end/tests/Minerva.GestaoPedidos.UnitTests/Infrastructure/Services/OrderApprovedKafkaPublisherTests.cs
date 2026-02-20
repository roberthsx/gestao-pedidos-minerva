using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Publishers;
using Moq;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Services;

/// <summary>
/// Testes do OrderApprovedKafkaPublisher: sucesso, comportamento quando Kafka retorna erro e serialização do payload externo (ACL).
/// </summary>
public class OrderApprovedKafkaPublisherTests
{
    private readonly Mock<IKafkaProducerService> _producerMock;
    private readonly OrderApprovedKafkaPublisher _sut;

    public OrderApprovedKafkaPublisherTests()
    {
        _producerMock = new Mock<IKafkaProducerService>();
        _sut = new OrderApprovedKafkaPublisher(_producerMock.Object);
    }

    private static Order CreateOrderWithId(int id)
    {
        var order = Order.Create(1, 1, DateTime.UtcNow, new[] { ("P", 1, 100m) });
        typeof(Order).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(order, id);
        return order;
    }

    [Fact]
    public async Task PublishOrderApprovedAsync_WhenOrderIdValid_AndProducerSucceeds_ReturnsTrue()
    {
        _producerMock
            .Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var order = CreateOrderWithId(10);
        var result = await _sut.PublishOrderApprovedAsync(order, CancellationToken.None);

        result.Should().BeTrue();
        _producerMock.Verify(
            x => x.TryProduceAsync(OrderApprovedKafkaPublisher.OrderApprovedTopic, It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishOrderApprovedAsync_WhenProducerFails_ReturnsFalse()
    {
        _producerMock
            .Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var order = CreateOrderWithId(1);
        var result = await _sut.PublishOrderApprovedAsync(order, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task PublishOrderApprovedAsync_WhenOrderNull_ReturnsFalseWithoutCallingProducer()
    {
        var result = await _sut.PublishOrderApprovedAsync(null!, CancellationToken.None);

        result.Should().BeFalse();
        _producerMock.Verify(
            x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishOrderApprovedAsync_WhenOrderIdZero_ReturnsFalseWithoutCallingProducer()
    {
        var order = Order.Create(1, 1, DateTime.UtcNow, new[] { ("P", 1, 100m) });

        var result = await _sut.PublishOrderApprovedAsync(order, CancellationToken.None);

        result.Should().BeFalse();
        _producerMock.Verify(
            x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishOrderApprovedAsync_PayloadIsSerializedWithOrderIdStatusAndApprovedAtUtc()
    {
        string? capturedPayload = null;
        _producerMock
            .Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyDictionary<string, byte[]>?, CancellationToken>((_, payload, __, ___) => capturedPayload = payload)
            .ReturnsAsync(true);

        var order = CreateOrderWithId(7);
        await _sut.PublishOrderApprovedAsync(order, CancellationToken.None);

        capturedPayload.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.TryGetProperty("orderId", out var orderId).Should().BeTrue();
        orderId.GetInt32().Should().Be(7);
        doc.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("Pago");
        doc.RootElement.TryGetProperty("approvedAtUtc", out var approvedAtUtc).Should().BeTrue();
        DateTime.TryParse(approvedAtUtc.GetString(), out _).Should().BeTrue();
    }
}
