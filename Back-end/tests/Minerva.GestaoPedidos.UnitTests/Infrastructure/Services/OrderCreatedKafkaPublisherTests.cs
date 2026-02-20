using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Publishers;
using Moq;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Services;

public class OrderCreatedKafkaPublisherTests
{
    private readonly Mock<IKafkaProducerService> _producerMock;
    private readonly Mock<ILogger<OrderCreatedKafkaPublisher>> _loggerMock;
    private readonly OrderCreatedKafkaPublisher _sut;

    public OrderCreatedKafkaPublisherTests()
    {
        _producerMock = new Mock<IKafkaProducerService>();
        _loggerMock = new Mock<ILogger<OrderCreatedKafkaPublisher>>();
        _sut = new OrderCreatedKafkaPublisher(_producerMock.Object, _loggerMock.Object);
    }

    private static Order CreateOrderWithId(int id)
    {
        var order = Order.Create(1, 1, DateTime.UtcNow, new[] { ("P", 1, 10m) });
        typeof(Order).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(order, id);
        return order;
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_WhenOrderIdValid_AndProducerSucceeds_ReturnsTrue()
    {
        _producerMock.Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var order = CreateOrderWithId(42);
        var result = await _sut.PublishOrderCreatedAsync(order, null, null, CancellationToken.None);
        result.Should().BeTrue();
        _producerMock.Verify(x => x.TryProduceAsync(OrderCreatedKafkaPublisher.OrderCreatedTopic, "42", It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_WhenProducerFails_ReturnsFalse()
    {
        _producerMock.Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var order = CreateOrderWithId(1);
        var result = await _sut.PublishOrderCreatedAsync(order, null, null, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_WhenOrderNull_ReturnsFalseWithoutCallingProducer()
    {
        var result = await _sut.PublishOrderCreatedAsync(null!, null, null, CancellationToken.None);
        result.Should().BeFalse();
        _producerMock.Verify(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_WhenOrderIdZero_ReturnsFalseWithoutCallingProducer()
    {
        var order = Order.Create(1, 1, DateTime.UtcNow, new[] { ("P", 1, 10m) });
        var result = await _sut.PublishOrderCreatedAsync(order, null, null, CancellationToken.None);
        result.Should().BeFalse();
        _producerMock.Verify(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_PayloadIsSerializedAsCamelCaseOrderId()
    {
        string? capturedPayload = null;
        _producerMock.Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, IReadOnlyDictionary<string, byte[]>?, CancellationToken>((_, __, payload, ___, ____) => capturedPayload = payload)
            .ReturnsAsync(true);
        var order = CreateOrderWithId(99);
        await _sut.PublishOrderCreatedAsync(order, null, null, CancellationToken.None);
        capturedPayload.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.TryGetProperty("orderId", out var orderId).Should().BeTrue();
        orderId.GetInt32().Should().Be(99);
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_WhenCorrelationIdProvided_AddsHeader()
    {
        IReadOnlyDictionary<string, byte[]>? capturedHeaders = null;
        _producerMock.Setup(x => x.TryProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, byte[]>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, IReadOnlyDictionary<string, byte[]>?, CancellationToken>((_, __, ___, headers, ____) => capturedHeaders = headers)
            .ReturnsAsync(true);
        var order = CreateOrderWithId(1);
        await _sut.PublishOrderCreatedAsync(order, "correlation-123", null, CancellationToken.None);
        capturedHeaders.Should().NotBeNull();
        capturedHeaders!.ContainsKey(OrderCreatedKafkaPublisher.HeaderCorrelationId).Should().BeTrue();
        System.Text.Encoding.UTF8.GetString(capturedHeaders[OrderCreatedKafkaPublisher.HeaderCorrelationId]).Should().Be("correlation-123");
    }
}
