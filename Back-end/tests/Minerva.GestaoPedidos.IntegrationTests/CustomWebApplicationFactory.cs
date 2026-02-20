using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Tests.Fakes;

namespace Minerva.GestaoPedidos.IntegrationTests;

/// <summary>
/// Factory que substitui Postgres por banco em memória e usa Fakes para repositórios de leitura e message bus.
/// A API de produção exige ConnectionStrings:Postgres; aqui injetamos uma connection string dummy e depois
/// substituímos o DbContext e os read repositories pelos Fakes do projeto de testes.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string EnvPostgres = "ConnectionStrings__Postgres";
    private const string EnvKafka = "Kafka__BootstrapServers";
    private const string EnvAspNetCore = "ASPNETCORE_ENVIRONMENT";

    /// <summary>Connection string dummy para AddInfrastructure não falhar; o DbContext é substituído em seguida por InMemory.</summary>
    private const string DummyPostgres = "Host=localhost;Database=test;Username=test;Password=test";

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(EnvAspNetCore, "Test");
        Environment.SetEnvironmentVariable(EnvPostgres, DummyPostgres);
        Environment.SetEnvironmentVariable(EnvKafka, "");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = DummyPostgres }));

        builder.ConfigureTestServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(IUserReadRepository) ||
                d.ServiceType == typeof(IOrderReadRepository)).ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("IntegrationTestDb"));
            services.AddScoped<IUserReadRepository>(sp => new InMemoryUserReadRepositoryAdapter(sp.GetRequiredService<IUserRepository>()));
            services.AddScoped<IOrderReadRepository, InMemoryOrderReadRepositoryAdapter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable(EnvAspNetCore, null);
            Environment.SetEnvironmentVariable(EnvPostgres, null);
            Environment.SetEnvironmentVariable(EnvKafka, null);
        }
        base.Dispose(disposing);
    }
}