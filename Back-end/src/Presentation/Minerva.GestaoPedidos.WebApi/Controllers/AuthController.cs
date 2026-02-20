using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;

namespace Minerva.GestaoPedidos.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequestDto> _validator;

    public AuthController(IAuthService authService, IValidator<LoginRequestDto> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    /// <summary>
    /// Login por número de registro e senha. O perfil é identificado pela API (não enviado pelo cliente).
    /// Retorno: accessToken, expiresIn e user (nome, perfil).
    /// Validação de presença dos campos (FluentValidation); falhas de autenticação retornam 401 com mensagem em PT.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequestDto? request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "O corpo da requisição é obrigatório." });

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { errors });
        }

        var result = await _authService.LoginAsync(
            request.RegistrationNumber!.Trim(),
            request.Password!,
            cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        var dto = result.Value!;
        return Ok(new
        {
            accessToken = dto.AccessToken,
            expiresIn = dto.ExpiresIn,
            user = new
            {
                name = dto.User.Name,
                role = dto.User.Role
            }
        });
    }
}