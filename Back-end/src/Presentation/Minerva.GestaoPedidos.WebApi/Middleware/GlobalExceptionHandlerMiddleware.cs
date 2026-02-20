using Microsoft.AspNetCore.Mvc;
using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.WebApi.Common;
using System.Text.Json;

namespace Minerva.GestaoPedidos.WebApi.Middleware;

/// <summary>
/// Captura exceções não tratadas do pipeline, registra com CorrelationId e retorna envelope ApiResponse (Success=false, Errors).
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private const string GenericErrorMessage =
        "Ocorreu um erro interno no servidor.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items[CorrelationMiddleware.CorrelationIdKey] as string;

        // Idempotência/Concorrência: log como Warning, sem stack trace
        if (exception is OrderAlreadyExistsException)
        {
            _logger.LogWarning("Concorrência/Idempotência: Tentativa de criação de pedido duplicado bloqueada. CorrelationId: {CorrelationId}", correlationId ?? "(n/a)");
        }
        else
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                [CorrelationMiddleware.CorrelationIdKey] = correlationId
            }))
            {
                _logger.LogError(exception, "Exceção não tratada ao processar {Path}. CorrelationId={CorrelationId}",
                    context.Request.Path, correlationId);
            }
        }

        var (statusCode, message, errors) = BuildApiResponseContent(context, exception);

        var envelope = ApiResponse<object>.Failure(message, errors);
        if (!_environment.IsProduction() && statusCode == StatusCodes.Status500InternalServerError && !string.IsNullOrEmpty(exception.StackTrace))
        {
            envelope.Errors ??= new List<string>();
            envelope.Errors.Add(exception.StackTrace);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(envelope, JsonOptions),
            context.RequestAborted).ConfigureAwait(false);
    }

    private (int statusCode, string message, IReadOnlyList<string>? errors) BuildApiResponseContent(
        HttpContext context, Exception exception)
    {
        if (exception is OrderAlreadyExistsException orderExists)
            return (StatusCodes.Status409Conflict, "Pedido já processado.", new[] { orderExists.Message });

        if (exception is InfrastructureException or ServiceUnavailableException)
        {
            var msg = exception is InfrastructureException infraEx ? infraEx.Message : InfrastructureException.DefaultMessage;
            return (StatusCodes.Status503ServiceUnavailable, "InfrastructureError", new[] { msg });
        }

        switch (exception)
        {
            case ValidationException validationEx:
                var validationErrors = validationEx.Errors.Values.SelectMany(v => v).ToList();
                return (StatusCodes.Status400BadRequest, "Um ou mais erros de validação ocorreram.", validationErrors);
            case BadRequestException badReq:
                return (StatusCodes.Status400BadRequest, badReq.Message, new[] { badReq.Message });
            case NotFoundException notFound:
                return (StatusCodes.Status404NotFound, notFound.Message, new[] { notFound.Message });
            case UnauthorizedAccessException:
                return (StatusCodes.Status401Unauthorized, exception.Message, new[] { exception.Message });
            case ConflictException conflict:
                return (StatusCodes.Status422UnprocessableEntity, conflict.Message, new[] { conflict.Message });
            case BusinessException business:
                return (StatusCodes.Status422UnprocessableEntity, business.Message, new[] { business.Message });
        }

        return (StatusCodes.Status500InternalServerError, GenericErrorMessage, new[] { GenericErrorMessage });
    }
}