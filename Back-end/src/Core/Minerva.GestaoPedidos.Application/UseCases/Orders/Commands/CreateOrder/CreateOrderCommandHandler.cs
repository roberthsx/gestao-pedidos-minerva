using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Application.Common.Constants;
using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Npgsql;
using System.Security.Cryptography;
using System.Text;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentConditionRepository _paymentConditionRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderCreatedPublisher _orderCreatedPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        ICustomerRepository customerRepository,
        IPaymentConditionRepository paymentConditionRepository,
        IOrderRepository orderRepository,
        IOrderCreatedPublisher orderCreatedPublisher,
        IMapper mapper,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _paymentConditionRepository = paymentConditionRepository;
        _orderRepository = orderRepository;
        _orderCreatedPublisher = orderCreatedPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new NotFoundException(string.Format(ApplicationMessages.Customer.NotFound, request.CustomerId));

        var paymentCondition = await _paymentConditionRepository.GetByIdAsync(request.PaymentConditionId, cancellationToken);
        if (paymentCondition is null)
            throw new NotFoundException(string.Format(ApplicationMessages.PaymentCondition.NotFound, request.PaymentConditionId));

        var itemsTuple = request.Items
            .Select(i => (i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = Order.Create(
            request.CustomerId,
            request.PaymentConditionId,
            request.OrderDate,
            itemsTuple);

        var idempotencyKey = ComputeOrderIdempotencyKey(order.CustomerId, order.PaymentConditionId, order.TotalAmount, order.OrderDate);
        order.SetIdempotencyKey(idempotencyKey);

        // Idempotência: evita duplicata mesmo quando o provedor (ex.: InMemory) não aplica UNIQUE.
        var existingByKey = await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingByKey != null)
        {
            _logger.LogInformation(ApplicationMessages.Order.IdempotencyDuplicateBlockedPreInsert, request.CorrelationId ?? "(n/a)", existingByKey.Id);
            throw new OrderAlreadyExistsException(existingByKey.Id);
        }

        Order created;
        try
        {
            created = await _orderRepository.AddAsync(order, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            var existing = await _orderRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing is null)
                throw;
            _logger.LogInformation(ApplicationMessages.Order.IdempotencyDuplicateBlocked, request.CorrelationId ?? "(n/a)", existing.Id);
            throw new OrderAlreadyExistsException(existing.Id);
        }

        try
        {
            var sent = await _orderCreatedPublisher.PublishOrderCreatedAsync(created, request.CorrelationId, request.CausationId, cancellationToken);
            if (sent)
                _logger.LogInformation(ApplicationMessages.Kafka.PublishOrderCreatedSuccess, created.Id);
            else
                _logger.LogWarning(ApplicationMessages.Kafka.PublishOrderCreatedWarning, created.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ApplicationMessages.Kafka.PublishOrderCreatedError, created.Id);
        }

        return _mapper.Map<OrderDto>(created);
    }

    /// <summary>Hash SHA256 para trava de idempotência: mesmo CustomerId + PaymentConditionId + TotalAmount + OrderDate (truncado ao dia) = mesma chave.</summary>
    private static string ComputeOrderIdempotencyKey(int customerId, int paymentConditionId, decimal totalAmount, DateTime orderDate)
    {
        var payload = $"{customerId}|{paymentConditionId}|{totalAmount:F2}|{orderDate:yyyy-MM-dd}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}

