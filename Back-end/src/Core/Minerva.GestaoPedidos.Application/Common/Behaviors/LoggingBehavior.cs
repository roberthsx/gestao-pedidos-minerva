using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minerva.GestaoPedidos.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var payloadJson = SerializeForLog(request);
        _logger.LogInformation(
            "Handling request {RequestName}. Payload: {Payload}",
            requestName,
            payloadJson);

        var response = await next().WaitAsync(cancellationToken);
        stopwatch.Stop();

        var responseJson = SerializeForLog(response);
        _logger.LogInformation(
            "Handled request {RequestName}. Response: {Response} em {ElapsedMilliseconds}ms",
            requestName,
            responseJson,
            stopwatch.ElapsedMilliseconds);

        return response;
    }

    /// <summary>Serializa para log em JSON legível, sem referências circulares.</summary>
    private static string SerializeForLog(object? obj)
    {
        if (obj == null)
            return "null";
        try
        {
            return JsonSerializer.Serialize(obj, JsonOptions);
        }
        catch (Exception)
        {
            return "(falha na serialização)";
        }
    }
}