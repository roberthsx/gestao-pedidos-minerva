using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Domain.Interfaces;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Tests.Fakes;
using Minerva.GestaoPedidos.WebApi;

namespace Minerva.GestaoPedidos.FunctionalTests.Infrastructure;

/// <summary>
/// Factory para testes funcionais: substitui Postgres por banco em memória e usa Fakes (projeto Tests).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string EnvPostgres = "ConnectionStrings__Postgres";
    private const string EnvKafka = "Kafka__BootstrapServers";
    private const string EnvAspNetCore = "ASPNETCORE_ENVIRONMENT";
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

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("FunctionalTestDb"));
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
