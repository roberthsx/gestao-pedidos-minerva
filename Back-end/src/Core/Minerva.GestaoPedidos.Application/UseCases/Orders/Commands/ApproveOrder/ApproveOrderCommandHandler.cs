using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Common.Constants;
using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.ApproveOrder;

public class ApproveOrderCommandHandler : IRequestHandler<ApproveOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderApprovedPublisher _orderApprovedPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<ApproveOrderCommandHandler> _logger;

    public ApproveOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderApprovedPublisher orderApprovedPublisher,
        IMapper mapper,
        ILogger<ApproveOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _orderApprovedPublisher = orderApprovedPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(ApproveOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException(string.Format(ApplicationMessages.Order.NotFound, request.OrderId));
        }

        if (order.Status != OrderStatus.Criado)
        {
            if (order.Status == OrderStatus.Pago)
                throw new BadRequestException(ApplicationMessages.Order.CannotApproveAlreadyPaid);
            if (order.Status == OrderStatus.Cancelado)
                throw new BadRequestException(ApplicationMessages.Order.CannotApproveCanceled);
            throw new BadRequestException(string.Format(ApplicationMessages.Order.CannotApproveInvalidStatus, order.Status));
        }

        if (!order.RequiresManualApproval)
            throw new BadRequestException(ApplicationMessages.Order.DoesNotRequireManualApproval);

        order.Approve(request.ApprovedBy);
        await _orderRepository.SaveApprovedOrderAsync(order, cancellationToken);

        try
        {
            var sent = await _orderApprovedPublisher.PublishOrderApprovedAsync(order, cancellationToken);
            if (!sent)
                _logger.LogWarning(ApplicationMessages.Kafka.PublishOrderApprovedWarning, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ApplicationMessages.Kafka.PublishOrderApprovedError, order.Id);
        }

        var orderWithDetails = await _orderRepository.GetByIdWithDetailsAsync(order.Id, cancellationToken);
        var resultOrder = orderWithDetails ?? order;
        return _mapper.Map<OrderDto>(resultOrder);
    }
}