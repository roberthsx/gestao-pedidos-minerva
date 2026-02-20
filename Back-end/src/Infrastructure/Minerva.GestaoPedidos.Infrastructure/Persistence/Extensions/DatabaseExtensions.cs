using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Infrastructure.Data;

namespace Minerva.GestaoPedidos.Infrastructure.Persistence.Extensions;

/// <summary>
/// Métodos de extensão para inicialização do banco (migrations e seed) na subida.
/// Encapsula lógica de retry e tratamento 42P07 para o host subir mesmo com o banco temporariamente indisponível.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DatabaseExtensions
{
    /// <summary>
    /// Aplica migrations do EF Core e executa seed (Profiles, Admin, PaymentConditions).
    /// Resolve <see cref="AppDbContext"/> e logger via novo <see cref="IServiceScope"/>.
    /// Em falha, registra aviso e retorna sem lançar para a API continuar subindo.
    /// Deve ser chamado com await para não bloquear a thread principal; não usar Task.Run.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        try
        {
            // Scope e DbContext são descartados ao sair do using, liberando o pool de conexões para os Health Checks
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Minerva.GestaoPedidos.Infrastructure.Persistence.Extensions.DatabaseExtensions");

                var isRelational = dbContext.Database.IsRelational();

                logger.LogInformation("Inicialização do banco: IsRelational={IsRelational}, Provider={Provider}",
                    isRelational, dbContext.Database.ProviderName ?? "(null)");

                if (!isRelational)
                {
                    logger.LogWarning("Migrations não aplicadas: banco não é relacional (ex.: InMemory). Defina ConnectionStrings:Postgres para usar Postgres.");
                    await DbInitializer.SeedAsync(dbContext, logger);
                    return;
                }

                const int maxAttempts = 2;
                const int delaySeconds = 2;
                var migrationsApplied = false;

                for (var attempt = 1; attempt <= maxAttempts && !migrationsApplied; attempt++)
                {
                    try
                    {
                        if (attempt > 1)
                            logger.LogInformation("Tentativa {Attempt}/{Max} de aplicar migrations...", attempt, maxAttempts);
                        else
                            logger.LogInformation("Aplicando migrations do Entity Framework no Postgres...");

                        await dbContext.Database.MigrateAsync();
                        logger.LogInformation("Migrations aplicadas com sucesso.");

                        await DbInitializer.SeedAsync(dbContext, logger);
                        migrationsApplied = true;
                    }
                    catch (Exception ex)
                    {
                        // 42P07 = relation already exists; 42701 = column already exists → schema criado por scripts (initdb.d); sincronizar histórico e aplicar só o pendente
                        var pgEx = ex;
                        while (pgEx != null && pgEx.GetType().FullName != "Npgsql.PostgresException")
                            pgEx = pgEx.InnerException;
                        var sqlState = pgEx?.GetType().GetProperty("SqlState")?.GetValue(pgEx) as string;
                        var isRelationExists = string.Equals(sqlState, "42P07", StringComparison.Ordinal);
                        var isColumnExists = string.Equals(sqlState, "42701", StringComparison.Ordinal);

                        if (isRelationExists || isColumnExists)
                        {
                            logger.LogWarning(
                                "Schema já existe no banco (ex.: scripts em /docker-entrypoint-initdb.d). Registrando migrations já aplicadas no histórico e aplicando apenas as pendentes.");
                            // Migrations já refletidas no 01_schema.sql: InitialCreate, AddOrderIdempotencyKey, AddOrderApprovedByApprovedAt.
                            // Não inserir RenameUserMatriculaToRegistrationNumber para que essa migration rode (script usa Matricula).
                            await dbContext.Database.ExecuteSqlRawAsync(
                                @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES
('20260217000234_InitialCreate', '8.0.21'),
('20260217134422_AddOrderIdempotencyKey', '8.0.21'),
('20260219000000_AddOrderApprovedByApprovedAt', '8.0.21')
ON CONFLICT (""MigrationId"") DO NOTHING;");
                            await dbContext.Database.MigrateAsync();
                            logger.LogInformation("Migrations pendentes aplicadas com sucesso.");
                            await DbInitializer.SeedAsync(dbContext, logger);
                            migrationsApplied = true;
                            break;
                        }

                        logger.LogWarning(ex,
                            "Tentativa {Attempt}/{Max} falhou ao conectar/aplicar migrations. Inner: {InnerMessage}",
                            attempt, maxAttempts, ex.InnerException?.Message ?? ex.Message);

                        if (attempt < maxAttempts)
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                }

                if (!migrationsApplied)
                    logger.LogWarning(
                        "Não foi possível aplicar migrations no momento. A API continuará iniciando, mas funcionalidades que dependem do banco podem falhar.");
            }
        }
        catch (Exception ex)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Minerva.GestaoPedidos.Infrastructure.Persistence.Extensions.DatabaseExtensions");
            logger.LogWarning(ex,
                "Não foi possível aplicar migrations no momento. A API continuará iniciando, mas funcionalidades que dependem do banco podem falhar.");
        }
    }
}
