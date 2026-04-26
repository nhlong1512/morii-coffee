using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class CachingConfiguration
{
    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("CachingConnectionString")
            ?? throw new InvalidOperationException("Redis connection string 'CachingConnectionString' is not configured.");

        // Register Redis cache with the DI container
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });
        return services;
    }
}
