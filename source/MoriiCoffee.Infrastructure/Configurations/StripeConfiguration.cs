using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.Services.Payment;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>
/// Binds the <see cref="StripeSettings"/> section from configuration and registers it as a
/// singleton. Performs a startup-time validation + logging of the live/test mode so operators
/// see immediately which mode the deployment is running in (FR-020, edge case "test vs live mode confusion").
/// </summary>
public static class StripeConfiguration
{
    /// <summary>Adds the Stripe settings to DI.</summary>
    public static IServiceCollection ConfigureStripe(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<StripeSettings>()
            .Bind(configuration.GetSection("Stripe"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<StripeSettings>>().Value);
        services.AddHostedService<StripeStartupDiagnosticsService>();

        return services;
    }
}
