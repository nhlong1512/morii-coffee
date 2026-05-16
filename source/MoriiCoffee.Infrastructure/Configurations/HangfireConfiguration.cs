using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>
/// Configures Hangfire with PostgreSQL storage backed by the existing MoriiCoffeeDb.
/// Hangfire creates and manages its own schema — no EF migration required.
/// </summary>
public static class HangfireConfiguration
{
    public static IServiceCollection ConfigureHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnectionString");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                InvisibilityTimeout = TimeSpan.FromMinutes(5)
            }));

        services.AddHangfireServer();

        return services;
    }
}
