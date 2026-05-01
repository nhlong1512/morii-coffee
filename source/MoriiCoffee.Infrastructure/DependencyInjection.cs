using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Infrastructure.Clock;
using MoriiCoffee.Infrastructure.Configurations;
using MoriiCoffee.Infrastructure.Persistence;
using MoriiCoffee.Infrastructure.Services;
using MoriiCoffee.Infrastructure.Services.Cart;
using MoriiCoffee.Infrastructure.Services.Email;
using MoriiCoffee.Infrastructure.Services.Order;

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
        services.ConfigureSettings(configuration);
        services.ConfigureControllers();
        services.ConfigureCors(appCors);
        services.ConfigureApplicationDbContext(configuration);
        services.ConfigureIdentity();
        services.ConfigureAuthentication(configuration);
        services.ConfigureMediatR();
        services.ConfigureMapper();
        services.ConfigureValidation();
        services.ConfigureSwagger();
        services.ConfigureStorage();
        services.ConfigureRedis(configuration);
        services.ConfigurePersistenceServices();
        services.ConfigureDependencyInjection();

        return services;
    }

    public static IServiceCollection ConfigureDependencyInjection(this IServiceCollection services)
    {
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ISerializeService, JsonSerializeService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, BrevoEmailService>();
        services.AddScoped<IFileService, AwsS3FileService>();
        services.AddScoped<ICartService, RedisCartService>();
        services.AddScoped<IOrderIdGenerator, OrderIdGenerator>();
        return services;
    }
}
