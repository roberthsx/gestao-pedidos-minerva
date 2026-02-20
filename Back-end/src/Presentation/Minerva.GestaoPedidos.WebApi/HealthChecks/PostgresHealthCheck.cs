using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Minerva.GestaoPedidos.WebApi.HealthChecks;

/// <summary>
/// Health check do PostgreSQL com timeout curto e mensagem em português.
/// Evita que o endpoint /health fique pendurado quando o banco está fora.
/// Todas as chamadas assíncronas passam o CancellationToken para respeitar o timeout do registro.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    private const int TimeoutSeconds = 3;
    private readonly string? _connectionString;
    private readonly ILogger<PostgresHealthCheck> _logger;

    public PostgresHealthCheck(IConfiguration configuration, ILogger<PostgresHealthCheck> logger)
    {
        _logger = logger;
        var cs = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _connectionString = null;
            return;
        }
        var builder = new NpgsqlConnectionStringBuilder(cs) { Timeout = TimeoutSeconds };
        _connectionString = builder.ConnectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[HealthCheck] postgres: início");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogDebug("[HealthCheck] postgres: fim (não configurado)");
            return HealthCheckResult.Healthy("Postgres não configurado (InMemory).");
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[HealthCheck] postgres: fim");
            return HealthCheckResult.Healthy("Conexão com o banco de dados OK.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[HealthCheck] postgres: cancelado (timeout)");
            return HealthCheckResult.Unhealthy("Falha na conexão com o banco de dados.");
        }
        catch (Exception)
        {
            _logger.LogDebug("[HealthCheck] postgres: fim (exceção)");
            return HealthCheckResult.Unhealthy("Falha na conexão com o banco de dados.");
        }
    }
}
