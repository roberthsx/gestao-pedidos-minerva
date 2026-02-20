using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Minerva.GestaoPedidos.WebApi.HealthChecks;

/// <summary>
/// Verifica apenas a conectividade com o broker Kafka via GetMetadata (sem publicar mensagens).
/// Evita falha de metadados e dispensa o uso de tópico dedicado para health check.
/// </summary>
public sealed class KafkaMetadataHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaMetadataHealthCheck>? _logger;

    public KafkaMetadataHealthCheck(string bootstrapServers, ILogger<KafkaMetadataHealthCheck>? logger = null)
    {
        _bootstrapServers = bootstrapServers ?? throw new ArgumentNullException(nameof(bootstrapServers));
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers,
                SocketTimeoutMs = 2000
            }).Build();

            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));
            if (metadata?.Brokers == null || metadata.Brokers.Count == 0)
            {
                _logger?.LogWarning("[HealthCheck] kafka: nenhum broker nos metadados.");
                return HealthCheckResult.Unhealthy("Broker Kafka retornou nenhum broker nos metadados.");
            }

            return await Task.FromResult(HealthCheckResult.Healthy(
                $"Broker Kafka acessível ({metadata.Brokers.Count} broker(s))")).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "[HealthCheck] kafka: falha ao obter metadados.");
            return HealthCheckResult.Unhealthy("Broker Kafka inacessível.", ex);
        }
    }
}
