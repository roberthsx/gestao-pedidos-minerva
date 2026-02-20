using Serilog.Context;

namespace Minerva.GestaoPedidos.WebApi.Middleware;

/// <summary>
/// Rastreamento distribuído: captura ou gera X-Correlation-ID e X-Causation-ID e adiciona ao LogContext (Serilog).
/// </summary>
public sealed class CorrelationMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CausationIdHeader = "X-Causation-ID";
    public const string CorrelationIdKey = "CorrelationId";
    public const string CausationIdKey = "CausationId";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        var causationId = context.Request.Headers[CausationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(causationId))
            causationId = Guid.NewGuid().ToString("N");

        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CausationIdHeader] = causationId;
        context.Items[CorrelationIdKey] = correlationId;
        context.Items[CausationIdKey] = causationId;

        using (LogContext.PushProperty(CorrelationIdKey, correlationId))
        using (LogContext.PushProperty(CausationIdKey, causationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}