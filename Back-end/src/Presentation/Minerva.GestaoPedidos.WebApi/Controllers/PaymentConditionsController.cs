using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.WebApi.Controllers;

/// <summary>
/// Lookup de condições de pagamento para alimentar campos de seleção no front-end.
/// GET /api/v1/payment-conditions (requer Authorization: Bearer token).
/// </summary>
[ApiController]
[Route("api/v1/payment-conditions")]
[Authorize]
public class PaymentConditionsController : ControllerBase
{
    private readonly IPaymentConditionReadRepository _readRepository;
    private readonly IMapper _mapper;

    public PaymentConditionsController(IPaymentConditionReadRepository readRepository, IMapper mapper)
    {
        _readRepository = readRepository;
        _mapper = mapper;
    }

    /// <summary>Lista todas as condições de pagamento (Id, Description, NumberOfInstallments) para dropdowns.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentConditionLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaymentConditionLookupDto>>> GetLookup(CancellationToken cancellationToken)
    {
        var list = await _readRepository.GetLookupAsync(cancellationToken).ConfigureAwait(false);
        var dtos = _mapper.Map<IReadOnlyList<PaymentConditionLookupDto>>(list);
        return Ok(dtos);
    }
}