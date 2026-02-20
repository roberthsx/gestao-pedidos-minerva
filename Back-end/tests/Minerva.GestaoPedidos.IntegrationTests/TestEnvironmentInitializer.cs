using System.Runtime.CompilerServices;

namespace Minerva.GestaoPedidos.IntegrationTests;

/// <summary>
/// Garante variáveis de ambiente de teste antes de qualquer host ser construído.
/// </summary>
internal static class TestEnvironmentInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", "");
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", "");
    }
}