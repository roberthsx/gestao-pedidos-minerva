using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minerva.GestaoPedidos.Domain.Constants;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Infrastructure.Data;

/// <summary>
/// Insere dados iniciais (Profiles, usuário administrativo, condições de pagamento)
/// quando as tabelas estão vazias. Executado após aplicação das migrations.
/// </summary>
public static class DbInitializer
{
    /// <summary>Número de registro padrão do usuário admin para login.</summary>
    public const string DefaultAdminRegistrationNumber = "admin";

    /// <summary>Senha padrão do admin (alterar em produção). Hash BCrypt.</summary>
    public const string DefaultAdminPassword = "Admin@123";

    public static async Task SeedAsync(AppDbContext context, ILogger? logger, CancellationToken cancellationToken = default)
    {
        if (await context.Profiles.AnyAsync(cancellationToken))
        {
            logger?.LogDebug("Profiles já existem; seed de dados iniciais ignorado.");
            return;
        }

        logger?.LogInformation("Iniciando seed de dados iniciais (Profiles, Admin, PaymentConditions).");

        // 1. Profiles (Admin, Gestão, Analista)
        var adminProfile = new Profile(ProfileCodes.Admin, "Admin");
        var gestaoProfile = new Profile(ProfileCodes.Gestao, "Gestão");
        var analistaProfile = new Profile(ProfileCodes.Analista, "Analista");

        context.Profiles.AddRange(adminProfile, gestaoProfile, analistaProfile);
        await context.SaveChangesAsync(cancellationToken);

        // 2. Usuário administrativo (registro/senha para login)
        var adminUser = new User("Admin", "Sistema", "admin@minerva.local", active: true);
        context.Users.Add(adminUser);
        var entry = context.Entry(adminUser);
        entry.Property("ProfileId").CurrentValue = adminProfile.Id;
        entry.Property("RegistrationNumber").CurrentValue = DefaultAdminRegistrationNumber;
        entry.Property("PasswordHash").CurrentValue = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword);
        await context.SaveChangesAsync(cancellationToken);

        logger?.LogInformation("Admin user created. Registration number: {RegistrationNumber}. Change password in production.", DefaultAdminRegistrationNumber);

        // 3. Condições de pagamento iniciais
        var conditions = new[]
        {
            new PaymentCondition("À vista", 1),
            new PaymentCondition("30 dias", 1),
            new PaymentCondition("30/60 dias", 2),
            new PaymentCondition("30/60/90 dias", 3)
        };
        context.PaymentConditions.AddRange(conditions);
        await context.SaveChangesAsync(cancellationToken);

        logger?.LogInformation("Seed concluído: {ProfileCount} perfis, 1 usuário admin, {ConditionCount} condições de pagamento.",
            await context.Profiles.CountAsync(cancellationToken),
            await context.PaymentConditions.CountAsync(cancellationToken));
    }
}
