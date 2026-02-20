using AutoMapper;
using FluentAssertions;
using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.Application.Common.Mappings;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.ApproveOrder;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Application.UseCases.Orders.Commands.ApproveOrder;

/// <summary>
/// Testes para ApproveOrderCommandHandler: branches de erro (pedido não encontrado, já aprovado, cancelado) e fluxo feliz.
/// </summary>
public class ApproveOrderCommandHandlerTests
{
    private static Order CreateOrderWithStatusCriado()
    {
        var order = Order.Create(1, 1, DateTime.UtcNow, new[] { ("P", 100, 100m) });
        order.SetIdempotencyKey("key-approve-test");
        return order;
    }

    private static Order CreateOrderWithStatusPago()
    {
        return Order.Create(1, 1, DateTime.UtcNow, new[] { ("P", 1, 100m) });
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ThrowsNotFoundException()
    {
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new ApproveOrderCommandHandler(
            orderRepo.Object,
            Mock.Of<Minerva.GestaoPedidos.Application.Contracts.IOrderApprovedPublisher>(),
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<ApproveOrderCommandHandler>>());

        var act = () => handler.Handle(new ApproveOrderCommand(999, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*não encontrado*");
    }

    [Fact]
    public async Task Handle_WhenOrderAlreadyPaid_ThrowsBadRequestException()
    {
        var order = CreateOrderWithStatusPago();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new ApproveOrderCommandHandler(
            orderRepo.Object,
            Mock.Of<Minerva.GestaoPedidos.Application.Contracts.IOrderApprovedPublisher>(),
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<ApproveOrderCommandHandler>>());

        var act = () => handler.Handle(new ApproveOrderCommand(order.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*já está pago*");
    }

    [Fact]
    public async Task Handle_WhenOrderCanceled_ThrowsBadRequestException()
    {
        var order = CreateOrderWithStatusCriado();
        order.Cancel();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new ApproveOrderCommandHandler(
            orderRepo.Object,
            Mock.Of<Minerva.GestaoPedidos.Application.Contracts.IOrderApprovedPublisher>(),
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<ApproveOrderCommandHandler>>());

        var act = () => handler.Handle(new ApproveOrderCommand(order.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*cancelado*");
    }

    [Fact]
    public async Task Handle_WhenOrderRequiresApproval_ApprovesAndReturnsDto()
    {
        var order = CreateOrderWithStatusCriado();
        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orderRepo.Setup(r => r.SaveApprovedOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

        var handler = new ApproveOrderCommandHandler(
            orderRepo.Object,
            Mock.Of<Minerva.GestaoPedidos.Application.Contracts.IOrderApprovedPublisher>(),
            mapper,
            Mock.Of<ILogger<ApproveOrderCommandHandler>>());

        var result = await handler.Handle(new ApproveOrderCommand(order.Id, "admin"), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.Status.Should().Be(nameof(OrderStatus.Pago));
        result.CustomerName.Should().BeEmpty();
        result.Items.Should().NotBeNull().And.HaveCount(1);
        order.ApprovedBy.Should().Be("admin");
        order.ApprovedAt.Should().NotBeNull();
        orderRepo.Verify(r => r.SaveApprovedOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenGetByIdWithDetailsReturnsOrder_MapsAndReturnsMappedDto()
    {
        var order = CreateOrderWithStatusCriado();
        var expectedDto = new OrderDto(1, 1, "C", 1, "À vista", DateTime.UtcNow, 10000m, "Pago", false, 0, null, "admin", DateTime.UtcNow, Array.Empty<OrderItemDto>());
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<OrderDto>(It.IsAny<Order>())).Returns(expectedDto);

        var orderRepo = new Mock<IOrderRepository>();
        orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orderRepo.Setup(r => r.SaveApprovedOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new ApproveOrderCommandHandler(
            orderRepo.Object,
            Mock.Of<Minerva.GestaoPedidos.Application.Contracts.IOrderApprovedPublisher>(),
            mapper.Object,
            Mock.Of<ILogger<ApproveOrderCommandHandler>>());

        var result = await handler.Handle(new ApproveOrderCommand(order.Id, "manager1"), CancellationToken.None);

        result.Should().BeSameAs(expectedDto);
        mapper.Verify(m => m.Map<OrderDto>(order), Times.Once);
    }
}
