using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>Binds strongly-typed settings classes to their corresponding configuration sections and registers them as singletons.</summary>
public static class SettingsConfiguration
{
    public static IServiceCollection ConfigureSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

        services.AddSingleton<JwtOptions>(jwtOptions!);

        return services;
    }
}
