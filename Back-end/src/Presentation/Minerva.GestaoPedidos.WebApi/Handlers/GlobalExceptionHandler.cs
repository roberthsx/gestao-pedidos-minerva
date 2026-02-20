using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Minerva.GestaoPedidos.Application.Common.Exceptions;

namespace Minerva.GestaoPedidos.WebApi.Handlers;

/// <summary>
/// Global exception handler using .NET 8+ IExceptionHandler and IProblemDetailsService (RFC 7807).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Pedido duplicado (idempotência): 409 Conflict com existingOrderId para o cliente exibir "Pedido já processado".
        if (exception is OrderAlreadyExistsException orderExists)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            httpContext.Response.ContentType = "application/json";
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                existingOrderId = orderExists.ExistingOrderId,
                message = "Pedido já processado."
            });
            await httpContext.Response.WriteAsync(json, cancellationToken);
            _logger.LogWarning("Pedido duplicado bloqueado por idempotência. ExistingOrderId={OrderId}", orderExists.ExistingOrderId);
            return true;
        }

        // Erro de infraestrutura: retorno padronizado 503 com JSON { "type": "InfrastructureError", "message": "..." }
        if (exception is InfrastructureException || exception is ServiceUnavailableException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            httpContext.Response.ContentType = "application/json";
            var message = exception is InfrastructureException infraEx
                ? infraEx.Message
                : InfrastructureException.DefaultMessage;
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "InfrastructureError",
                message = message
            });
            await httpContext.Response.WriteAsync(json, cancellationToken);
            _logger.LogWarning(exception, "Infraestrutura indisponível ao processar {Path}", httpContext.Request.Path);
            return true;
        }

        var (statusCode, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Um ou mais erros de validação ocorreram.", "A requisição é inválida."),
            BadRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida", exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, "Não encontrado", exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autorizado", exception.Message),
            ConflictException => (StatusCodes.Status422UnprocessableEntity, "Conflito", exception.Message),
            BusinessException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.", "Ocorreu um erro interno no servidor.")
        };

        if (exception is ValidationException)
            _logger.LogWarning(exception, "Erro de validação ao processar {Path}", httpContext.Request.Path);
        else if (exception is ConflictException)
            _logger.LogWarning(exception, "Conflito de negócio ao processar {Path}", httpContext.Request.Path);
        else if (exception is not NotFoundException && exception is not UnauthorizedAccessException)
            _logger.LogError(exception, "Exceção não tratada ao processar {Path}", httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
            problemDetails.Extensions["errors"] = validationException.Errors;

        if (_environment.IsDevelopment())
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;

        httpContext.Response.StatusCode = statusCode;

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        };

        await problemDetailsService.WriteAsync(context);
        return true;
    }
}