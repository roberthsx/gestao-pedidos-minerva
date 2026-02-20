using System.Net;
using HealthChecks.UI.Client;
using Minerva.GestaoPedidos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Prioridade para variáveis de ambiente (docker-compose sobrescreve appsettings).
builder.Configuration.AddEnvironmentVariables();

// Infraestrutura necessária ao Worker: PostgreSQL, Kafka (Consumer + Producer)
builder.Services.AddWorkerInfrastructure(builder.Configuration);

// Hosted service: consome order-created e cria DeliveryTerms no Postgres (padrão de tópicos com hífen).
builder.Services.AddHostedService<Minerva.GestaoPedidos.Worker.OrderCreatedKafkaConsumerHostedService>();

// Health Checks: liveness + dependências (Postgres, Kafka)
var healthBuilder = builder.Services.AddHealthChecks();
healthBuilder.AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Worker is running"), tags: new[] { "live" });

var postgresCs = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(postgresCs))
    healthBuilder.AddNpgSql(postgresCs, name: "postgres", tags: new[] { "db", "ready" });

// Kafka: valida conectividade com o broker (metadata), não apenas existência do processo
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"];
if (!string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    healthBuilder.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
        "kafka",
        sp => new Minerva.GestaoPedidos.Worker.HealthChecks.KafkaConnectivityHealthCheck(
            kafkaBootstrap,
            sp.GetService<ILogger<Minerva.GestaoPedidos.Worker.HealthChecks.KafkaConnectivityHealthCheck>>()),
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "messaging", "ready" }));
}

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("[Worker] Minerva.GestaoPedidos.Worker iniciado. Tópico: order-created (DLQ: order-created-dlq).");

// Verificação de DNS: garante que o container resolve o host do Kafka antes do consumer iniciar (handshake).
var kafkaBootstrapForDns = builder.Configuration["Kafka:BootstrapServers"];
if (!string.IsNullOrWhiteSpace(kafkaBootstrapForDns))
{
    var kafkaHost = kafkaBootstrapForDns.Split(',')[0].Trim().Split(':')[0].Trim();
    try
    {
        var addresses = await Dns.GetHostAddressesAsync(kafkaHost);
        startupLogger.LogInformation("DNS: host Kafka {Host} resolvido para {Addresses}.",
            kafkaHost, addresses.Length > 0 ? string.Join(", ", addresses.Select(a => a.ToString())) : "(nenhum)");
        if (addresses.Length == 0)
            startupLogger.LogWarning("DNS: nenhum endereço retornado para {Host}. Consumer pode falhar no handshake.", kafkaHost);
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(ex, "DNS: falha ao resolver {Host}. Consumer pode falhar no handshake. Verifique rede/Docker.", kafkaHost);
    }
}

app.MapGet("/", () => Results.Ok(new { service = "Minerva.GestaoPedidos.Worker", status = "running" }));

// Liveness: orquestrador verifica se o processo está vivo
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = static check => check.Tags.Contains("live", StringComparer.OrdinalIgnoreCase)
});

// Readiness: estado de cada dependência (Postgres, Kafka)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = _ => true
});

app.Run();
