using HealthChecks.UI.Client;
using Minerva.GestaoPedidos.Application;
using Minerva.GestaoPedidos.Infrastructure;
using Minerva.GestaoPedidos.Infrastructure.Persistence.Extensions;
using Minerva.GestaoPedidos.WebApi.HealthChecks;
using Minerva.GestaoPedidos.WebApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] CorrelationId={CorrelationId} CausationId={CausationId} {Message:lj}{NewLine}{Exception}");

    // Em ambientes que não sejam Development, suprime Warning/Error do Kafka para não poluir o log
    var env = context.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
    if (!"Development".Equals(env, StringComparison.OrdinalIgnoreCase))
        configuration.MinimumLevel.Override("Confluent.Kafka", LogEventLevel.Fatal).MinimumLevel.Override("rdkafka", LogEventLevel.Fatal);
});

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Minerva.GestaoPedidos.WebApi.Filters.ApiResponseEnvelopeFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minerva Gestão de Pedidos",
        Version = "v1",
        Description = "API de gestão e aprovação de pedidos (V1). Base path: **api/v1**. Use POST **api/v1/auth/login** para obter o token e clique em **Authorize** para informá-lo. Respostas 2xx vêm no envelope ApiResponse (success, data, message, errors)."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT. Obtenha o token em **POST /api/auth/login**. No campo abaixo cole **apenas o token** (começa com eyJ...), sem escrever \"Bearer\" — o Swagger já adiciona o prefixo.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Problem Details (RFC 7807) - resposta padronizada via GlobalExceptionHandlerMiddleware
builder.Services.AddProblemDetails();

// Application & Infrastructure services
// Pass Infrastructure assembly so MediatR registers handlers from Infrastructure
builder.Services.AddApplication(typeof(Minerva.GestaoPedidos.Infrastructure.DependencyInjection).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSecurity(builder.Configuration);

// CORS: permitir todas as origens (desenvolvimento/integração; em produção restrinja por origem)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader();
    });
});

// Health Checks: Liveness, PostgreSQL, custom Startup (migrations)
var healthBuilder = builder.Services.AddHealthChecks();

// Liveness: auto-verificação da API (sempre ativa)
healthBuilder.AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API em execução"), tags: new[] { "live" });

// PostgreSQL (banco de escrita): timeout 3s e mensagem em PT; quando fora, /health responde rápido como Unhealthy
var postgresCs = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(postgresCs))
{
    healthBuilder.AddCheck<PostgresHealthCheck>("postgres", failureStatus: null, tags: new[] { "db", "ready" }, timeout: TimeSpan.FromSeconds(3));
}

// Kafka: apenas conectividade com o broker (GetMetadata), sem publicar em tópico — evita falha de metadados
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"];
if (!string.IsNullOrWhiteSpace(kafkaBootstrap))
{
    builder.Services.AddSingleton<KafkaMetadataHealthCheck>(sp =>
        new KafkaMetadataHealthCheck(kafkaBootstrap, sp.GetService<ILogger<KafkaMetadataHealthCheck>>()));
    healthBuilder.AddCheck<KafkaMetadataHealthCheck>("kafka", failureStatus: null, tags: new[] { "messaging", "ready" }, timeout: TimeSpan.FromSeconds(5));
}

// Custom: verifica se migrações do EF foram aplicadas (scope interno; não bloqueia)
healthBuilder.AddCheck<StartupHealthCheck>("startup_migrations", failureStatus: null, tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

await app.ApplyMigrationsAsync();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("[API] Minerva.GestaoPedidos.WebApi iniciado. Endpoints: /swagger, /health, /health/live.");

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// CorrelationId/CausationId no LogContext do Serilog no início do pipeline (todas as linhas de log exibem o ID)
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoints por último (resiliência): sempre respondem; execução em paralelo (padrão do HealthCheckService)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = static check => check.Tags.Contains("live", StringComparer.OrdinalIgnoreCase)
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate = _ => true
});

await app.RunAsync();

// Make Program accessible for WebApplicationFactory (protected ctor satisfies Sonar: class not meant to be instantiated directly)
public partial class Program
{
    protected Program() { }
}
