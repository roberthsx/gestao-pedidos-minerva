using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Infrastructure.Identity.Configurations;
using Minerva.GestaoPedidos.Infrastructure.Identity.Services;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Domain;
using Minerva.GestaoPedidos.Infrastructure.Messaging.InMemory;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Abstractions;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Core;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Publishers;
using Minerva.GestaoPedidos.Infrastructure.Messaging.Kafka.Handlers;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Npgsql;

namespace Minerva.GestaoPedidos.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>
    /// Ponto de extensão para registrar todos os serviços da camada de infraestrutura.
    /// </summary>
    /// <param name="services">Coleção de serviços do host.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A mesma instância de <see cref="IServiceCollection"/> para encadeamento.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is required. Configure ConnectionStrings:Postgres.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 30,
            CommandTimeout = 30
        };

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(builder.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorCodesToAdd: null);
            });
        });

        // Write-side repositories (Postgres via EF Core)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPaymentConditionRepository, PaymentConditionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Read repositories (leitura com AsNoTracking)
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
        services.AddScoped<IPaymentConditionReadRepository, PaymentConditionReadRepository>();
        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();

        // Domain service (email uniqueness, etc.)
        services.AddScoped<IUserDomainService, UserDomainService>();

        // Authentication
        services.AddMemoryCache();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IAuthUserStore, DbAuthUserStore>();
        services.AddScoped<IAuthService, AuthService>();

        // Kafka producer (obrigatório em produção; quando não configurado usa NoOp para evitar falha em startup)
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"];
        if (!string.IsNullOrWhiteSpace(kafkaBootstrap))
        {
            services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
            services.AddSingleton<IOrderCreatedPublisher, OrderCreatedKafkaPublisher>();
            services.AddSingleton<IOrderApprovedPublisher, OrderApprovedKafkaPublisher>();
        }
        else
        {
            services.AddSingleton<IOrderCreatedPublisher, NoOpOrderCreatedPublisher>();
            services.AddSingleton<IOrderApprovedPublisher, NoOpOrderApprovedPublisher>();
        }

        return services;
    }

    /// <summary>
    /// Registra autenticação JWT e autorização. Prioriza a variável de ambiente JWT_SECRET; fallback em appsettings (seção Jwt).
    /// </summary>
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                    ?? jwtSection["Secret"]
                    ?? "MinervaFoods_Super_Secret_Key_2026_TechLead";
        var issuer = jwtSection["Issuer"] ?? "Minerva.GestaoPedidos";
        var audience = jwtSection["Audience"] ?? "Minerva.Frontend";
        var key = Encoding.UTF8.GetBytes(secret);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var loggerFactory = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("Minerva.GestaoPedidos.Infrastructure.Security");
                        logger.LogWarning(ctx.Exception, "Falha na validação do JWT: {Message}", ctx.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        // RBAC: [Authorize(Roles = "ADMIN,MANAGER")] etc. Retorna 403 Forbidden quando o usuário está autenticado mas não tem a role.
        // As roles (ADMIN, MANAGER, ANALYST) são emitidas no JWT em AuthService; o mapeamento Profile → Role está em DbAuthUserStore.MapProfileCodeToRole.
        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Registra apenas as dependências necessárias para o Worker (PostgreSQL, Kafka).
    /// Usado pelo projeto Minerva.GestaoPedidos.Worker; não inclui Auth, repositórios de escrita nem os hosted services (registrados no Worker).
    /// AppDbContext e IOrderCreatedMessageHandler são Scoped; o HostedService cria escopo e resolve o handler por mensagem.
    /// </summary>
    public static IServiceCollection AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required for the Worker.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(postgresConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorCodesToAdd: null);
            });
        });

        var kafkaBootstrap = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers is required for the Worker.");
        services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
        services.AddScoped<IOrderCreatedMessageHandler, OrderCreatedHandler>();

        return services;
    }
}

