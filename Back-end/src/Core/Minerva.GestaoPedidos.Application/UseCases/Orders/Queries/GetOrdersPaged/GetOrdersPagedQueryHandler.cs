using AutoMapper;
using MediatR;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Interfaces;

namespace Minerva.GestaoPedidos.Application.UseCases.Orders.Queries.GetOrdersPaged;

public class GetOrdersPagedQueryHandler : IRequestHandler<GetOrdersPagedQuery, PagedResponse<OrderDto>>
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;

    private readonly IOrderReadRepository _orderReadRepository;
    private readonly IMapper _mapper;

    public GetOrdersPagedQueryHandler(IOrderReadRepository orderReadRepository, IMapper mapper)
    {
        _orderReadRepository = orderReadRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<OrderDto>> Handle(GetOrdersPagedQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? DefaultPageNumber : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : request.PageSize;

        // Mapeamento seguro: string da API → OrderStatus? (validação já garantiu valor válido quando preenchido)
        OrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status.Trim(), ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var (items, totalCount) = await _orderReadRepository.GetPagedAsync(
            statusFilter,
            request.DateFrom,
            request.DateTo,
            pageNumber,
            pageSize,
            cancellationToken);

        var dtos = _mapper.Map<List<OrderDto>>(items);

        return new PagedResponse<OrderDto>(dtos, totalCount, pageNumber, pageSize);
    }
}