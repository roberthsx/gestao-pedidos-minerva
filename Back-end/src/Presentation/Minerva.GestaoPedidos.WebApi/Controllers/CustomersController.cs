using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.WebApi.Controllers;

/// <summary>
/// Lookup de clientes para alimentar campos de seleção no front-end.
/// GET /api/v1/customers (requer Authorization: Bearer token).
/// </summary>
[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerReadRepository _customerReadRepository;
    private readonly IMapper _mapper;

    public CustomersController(ICustomerReadRepository customerReadRepository, IMapper mapper)
    {
        _customerReadRepository = customerReadRepository;
        _mapper = mapper;
    }

    /// <summary>Lista todos os clientes (Id, Name) para dropdowns.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerLookupDto>>> GetLookup(CancellationToken cancellationToken)
    {
        var list = await _customerReadRepository.GetLookupAsync(cancellationToken).ConfigureAwait(false);
        var dtos = _mapper.Map<IReadOnlyList<CustomerLookupDto>>(list);
        return Ok(dtos);
    }
}