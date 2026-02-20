using AutoMapper;
using FluentAssertions;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Application.UseCases.Orders.Queries.GetOrdersPaged;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Domain.ReadModels;
using Moq;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Application.UseCases.Orders.Queries.GetOrdersPaged;

/// <summary>
/// Testes para GetOrdersPagedQueryHandler: paginação e filtros sobre repositório mockado.
/// </summary>
public class GetOrdersPagedQueryHandlerTests
{
    private readonly Mock<IOrderReadRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;

    public GetOrdersPagedQueryHandlerTests()
    {
        _repoMock = new Mock<IOrderReadRepository>();
        _mapperMock = new Mock<IMapper>();
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedResponseWithMappedItems()
    {
        var readModels = new List<OrderReadModel>
        {
            new() { OrderId = 1, CustomerName = "C1", TotalAmount = 100m, Status = nameof(OrderStatus.Pago) }
        };
        var dtos = new List<OrderDto>
        {
            new(1, 1, "C1", 1, "À vista", DateTime.UtcNow, 100m, "Pago", false, 0, null, null, null, Array.Empty<OrderItemDto>())
        };
        _repoMock
            .Setup(r => r.GetPagedAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((readModels, 1));
        _mapperMock.Setup(m => m.Map<List<OrderDto>>(It.IsAny<object>())).Returns(dtos);

        var handler = new GetOrdersPagedQueryHandler(_repoMock.Object, _mapperMock.Object);
        var query = new GetOrdersPagedQuery(null, null, null, 1, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().HaveCount(1);
        var first = result.Items!.First();
        first.Id.Should().Be(1);
        first.CustomerName.Should().Be("C1");
    }

    [Fact]
    public async Task Handle_WhenPageNumberZero_UsesDefaultPageOne()
    {
        _repoMock
            .Setup(r => r.GetPagedAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<OrderReadModel>(), 0));

        var handler = new GetOrdersPagedQueryHandler(_repoMock.Object, _mapperMock.Object);
        var query = new GetOrdersPagedQuery(null, null, null, 0, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        _repoMock.Verify(r => r.GetPagedAsync(It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 20, It.IsAny<CancellationToken>()));
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenPageSizeZero_UsesDefaultPageSize20()
    {
        _repoMock
            .Setup(r => r.GetPagedAsync(null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<OrderReadModel>(), 0));

        var handler = new GetOrdersPagedQueryHandler(_repoMock.Object, _mapperMock.Object);
        var query = new GetOrdersPagedQuery(null, null, null, 1, 0);

        var result = await handler.Handle(query, CancellationToken.None);

        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WhenStatusProvided_PassesParsedFilterToRepo()
    {
        OrderStatus? capturedStatus = null;
        _repoMock
            .Setup(r => r.GetPagedAsync(It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatus?, DateTime?, DateTime?, int, int, CancellationToken>((s, _, _, _, _, _) => capturedStatus = s)
            .ReturnsAsync((new List<OrderReadModel>(), 0));

        var handler = new GetOrdersPagedQueryHandler(_repoMock.Object, _mapperMock.Object);
        var query = new GetOrdersPagedQuery("Criado", null, null, 1, 20);

        await handler.Handle(query, CancellationToken.None);

        capturedStatus.Should().Be(OrderStatus.Criado);
    }

    [Fact]
    public async Task Handle_WhenStatusInvalid_PassesNullStatusFilter()
    {
        OrderStatus? capturedStatus = null;
        _repoMock
            .Setup(r => r.GetPagedAsync(It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatus?, DateTime?, DateTime?, int, int, CancellationToken>((s, _, _, _, _, _) => capturedStatus = s)
            .ReturnsAsync((new List<OrderReadModel>(), 0));

        var handler = new GetOrdersPagedQueryHandler(_repoMock.Object, _mapperMock.Object);
        var query = new GetOrdersPagedQuery("InvalidStatus", null, null, 1, 20);

        await handler.Handle(query, CancellationToken.None);

        capturedStatus.Should().BeNull();
    }
}
