using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Infrastructure.Data;

namespace Minerva.GestaoPedidos.WebApi.HealthChecks;

/// <summary>
/// Verifica se o banco de dados está acessível e se as migrações do EF Core foram aplicadas.
/// Usa IServiceProvider e cria um scope em cada execução para evitar deadlock com contexto
/// em transação (ex.: migration) e para não reutilizar contexto Scoped em Health Check Singleton.
/// </summary>
public sealed class StartupHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupHealthCheck> _logger;

    public StartupHealthCheck(IServiceProvider serviceProvider, ILogger<StartupHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[HealthCheck] startup_migrations: início");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("[HealthCheck] startup_migrations: fim");

            if (canConnect)
                return HealthCheckResult.Healthy("Banco acessível e migrações aplicadas (ou InMemory ativo).");

            return HealthCheckResult.Unhealthy("Migrations pendentes ou banco inacessível.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[HealthCheck] startup_migrations: cancelado (timeout)");
            return HealthCheckResult.Unhealthy("Migrations pendentes ou banco inacessível (timeout).");
        }
        catch (Exception)
        {
            _logger.LogDebug("[HealthCheck] startup_migrations: fim (exceção)");
            return HealthCheckResult.Unhealthy("Migrations pendentes ou banco inacessível.");
        }
    }
}
