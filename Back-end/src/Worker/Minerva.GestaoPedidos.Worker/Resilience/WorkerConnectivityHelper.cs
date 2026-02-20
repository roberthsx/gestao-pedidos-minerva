using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Npgsql;
using Polly;
using Polly.Retry;

namespace Minerva.GestaoPedidos.Worker.Resilience;

/// <summary>
/// Utilitários de resiliência para o consumer: verificação de conectividade (Postgres + Kafka) e política de retry.
/// </summary>
internal static class WorkerConnectivityHelper
{
    private const int MaxRetryAttempts = 3;

    public static async Task<bool> CanConnectAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var postgresCs = configuration.GetConnectionString("Postgres");
        var postgresCsForLog = MaskPasswordInConnectionString(postgresCs);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogWarning("CanConnectAsync: Postgres recusou conexão. String (senha mascarada): {ConnectionString}", postgresCsForLog);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CanConnectAsync: Postgres indisponível. String (senha mascarada): {ConnectionString}.", postgresCsForLog);
            return false;
        }

        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            return false;

        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrapServers,
                SocketTimeoutMs = 5000
            }).Build();
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
            if (metadata?.Brokers == null || metadata.Brokers.Count == 0)
            {
                logger.LogWarning("CanConnectAsync: Kafka retornou metadata sem brokers. BootstrapServers: {BootstrapServers}.", bootstrapServers);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CanConnectAsync: Kafka indisponível. BootstrapServers: {BootstrapServers}.", bootstrapServers);
            return false;
        }

        return true;
    }

    public static ResiliencePipeline CreateOrderCreatedRetryPolicy(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "Retry {Attempt}/{Max} ao processar order-created.",
                        args.AttemptNumber + 1,
                        MaxRetryAttempts);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public static string MaskPasswordInConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "(não configurada)";
        const string passwordKey = "Password=";
        var idx = connectionString.IndexOf(passwordKey, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return connectionString;
        var start = idx + passwordKey.Length;
        var end = connectionString.IndexOf(';', start);
        if (end < 0)
            end = connectionString.Length;
        return connectionString[..start] + "***" + connectionString[end..];
    }
}
