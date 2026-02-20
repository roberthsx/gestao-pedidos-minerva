using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Minerva.GestaoPedidos.Application.Common.Behaviors;

namespace Minerva.GestaoPedidos.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <param name="additionalAssemblies">Optional assemblies to scan for handlers (e.g. Infrastructure for INotificationHandler).</param>
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            foreach (var a in additionalAssemblies)
                cfg.RegisterServicesFromAssembly(a);
        });

        services.AddAutoMapper(assembly);

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
