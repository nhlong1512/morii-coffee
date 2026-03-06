using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Infrastructure.Clock;
using MoriiCoffee.Infrastructure.Configurations;
using MoriiCoffee.Infrastructure.Persistence;

namespace MoriiCoffee.Infrastructure;

/// <summary>
/// Central dependency injection entry point for the MoriiCoffee API.
/// Handles service registration only — HTTP pipeline configuration is in Presentation layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection ConfigureInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string appCors)
    {
        services.ConfigureControllers();
        services.ConfigureCors(appCors);
        services.ConfigureApplicationDbContext(configuration);
        services.ConfigureMediatR();
        services.ConfigureMapper();
        services.ConfigureValidation();
        services.ConfigureSwagger();
        services.ConfigurePersistenceServices();
        services.ConfigureDependencyInjection();

        return services;
    }

    public static IServiceCollection ConfigureDependencyInjection(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        return services;
    }
}
