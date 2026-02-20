using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.CreateOrder;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Moq;
using System.Reflection;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.UseCases;

/// <summary>
/// Testes do CreateOrderCommandHandler: validação de regras de negócio e integração com publicação no Kafka.
/// </summary>
public class CreateOrderCommandHandlerTests
{
    private static readonly DateTime OrderDate = new(2026, 2, 17, 15, 56, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ThrowsNotFoundException()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var paymentRepo = new Mock<IPaymentConditionRepository>();
        paymentRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentCondition("À vista", 1));

        var orderRepo = new Mock<IOrderRepository>();
        var kafkaPublisher = new Mock<IOrderCreatedPublisher>();
        var logger = Mock.Of<ILogger<CreateOrderCommandHandler>>();
        var handler = new CreateOrderCommandHandler(
            customerRepo.Object,
            paymentRepo.Object,
            orderRepo.Object,
            kafkaPublisher.Object,
            Mock.Of<IMapper>(),
            logger);

        var command = new CreateOrderCommand(
            CustomerId: 999,
            PaymentConditionId: 1,
            OrderDate,
            Items: new List<CreateOrderItemCommand> { new("Produto", 1, 10m) },
            CorrelationId: null,
            CausationId: null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Cliente '999' não encontrado*");
    }

    [Fact]
    public async Task Handle_WhenPaymentConditionNotFound_ThrowsNotFoundException()
    {
        var customer = new Customer("Cliente", "c@c.com");
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var paymentRepo = new Mock<IPaymentConditionRepository>();
        paymentRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentCondition?)null);

        var orderRepo = new Mock<IOrderRepository>();
        var kafkaPublisher = new Mock<IOrderCreatedPublisher>();
        var logger = Mock.Of<ILogger<CreateOrderCommandHandler>>();
        var handler = new CreateOrderCommandHandler(
            customerRepo.Object,
            paymentRepo.Object,
            orderRepo.Object,
            kafkaPublisher.Object,
            Mock.Of<IMapper>(),
            logger);

        var command = new CreateOrderCommand(
            CustomerId: 1,
            PaymentConditionId: 888,
            OrderDate,
            Items: new List<CreateOrderItemCommand> { new("Produto", 1, 10m) },
            CorrelationId: null,
            CausationId: null);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Condição de pagamento '888' não encontrada*");
    }

    [Fact]
    public async Task Handle_WhenOrderCreated_ShouldCallPublishOrderCreatedAsyncExactlyOnce()
    {
        // Arrange
        var customer = new Customer("Atacado S.A.", "atacado@example.com");
        var paymentCondition = new PaymentCondition("Cartão BNDES", 1);

        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var paymentRepo = new Mock<IPaymentConditionRepository>();
        paymentRepo
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentCondition);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) =>
            {
                SetOrderId(o, 1);
                return o;
            });

        var kafkaPublisher = new Mock<IOrderCreatedPublisher>();
        kafkaPublisher
            .Setup(x => x.PublishOrderCreatedAsync(It.IsAny<Order>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var expectedDto = new OrderDto(1, 4, string.Empty, 13, string.Empty, OrderDate, 10000m, "Criado", true, 0, null, null, null, new List<OrderItemDto> { new("pct Frango passarinho 1k (20 unidades)", 50, 200m, 10000m) });
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>())).Returns(expectedDto);

        var logger = Mock.Of<ILogger<CreateOrderCommandHandler>>();
        var handler = new CreateOrderCommandHandler(
            customerRepo.Object,
            paymentRepo.Object,
            orderRepo.Object,
            kafkaPublisher.Object,
            mapper.Object,
            logger);

        var command = new CreateOrderCommand(
            CustomerId: 4,
            PaymentConditionId: 13,
            OrderDate,
            Items: new List<CreateOrderItemCommand> { new("pct Frango passarinho 1k (20 unidades)", 50, 200m) },
            CorrelationId: "test-correlation",
            CausationId: "test-causation");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        kafkaPublisher.Verify(
            x => x.PublishOrderCreatedAsync(It.Is<Order>(o => o.Id == 1), "test-correlation", "test-causation", It.IsAny<CancellationToken>()),
            Times.Once,
            "o método Publish do Kafka deve ser chamado exatamente uma vez após a criação do pedido");
    }

    private static void SetOrderId(Order order, int id)
    {
        var prop = typeof(Order).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        prop!.SetValue(order, id);
    }
}
