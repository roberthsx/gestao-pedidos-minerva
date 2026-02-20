namespace Minerva.GestaoPedidos.WebApi.Middleware;

/// <summary>
/// Registra ao final do pipeline: RequestPath, QueryString e StatusCode da resposta para rastreio com CorrelationId.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context).ConfigureAwait(false);

        var path = context.Request.Path.Value ?? "(null)";
        var statusCode = context.Response.StatusCode;

        // Silencia logs de GET /health e /health/live com 200 para não poluir o console
        if (statusCode == 200 && context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && (path.Equals("/health", StringComparison.OrdinalIgnoreCase) || path.Equals("/health/live", StringComparison.OrdinalIgnoreCase)))
            return;

        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        _logger.LogInformation(
            "HTTP {StatusCode} {RequestPath}{QueryString}",
            statusCode,
            path,
            queryString);
    }
}