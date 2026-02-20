using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Minerva.GestaoPedidos.Worker.HealthChecks;

/// <summary>
/// Valida conectividade real com o broker Kafka (metadata request), não apenas existência do processo.
/// </summary>
public sealed class KafkaConnectivityHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaConnectivityHealthCheck>? _logger;

    public KafkaConnectivityHealthCheck(string bootstrapServers, ILogger<KafkaConnectivityHealthCheck>? logger = null)
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
                SocketTimeoutMs = 5000
            }).Build();

            // Solicita metadados ao broker para validar conectividade
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
            if (metadata?.Brokers == null || metadata.Brokers.Count == 0)
            {
                _logger?.LogWarning("Health check Kafka: nenhum broker nos metadados.");
                return HealthCheckResult.Unhealthy("Broker Kafka retornou nenhum broker nos metadados.");
            }

            return await Task.FromResult(HealthCheckResult.Healthy(
                $"Broker Kafka acessível ({metadata.Brokers.Count} broker(s), bootstrap: {_bootstrapServers})")).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Health check Kafka falhou: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy("Broker Kafka inacessível.", ex);
        }
    }
}
