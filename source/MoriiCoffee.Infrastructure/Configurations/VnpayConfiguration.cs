using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.Services.Payment;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class VnpayConfiguration
{
    public static IServiceCollection ConfigureVnpay(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VnpaySettings>().Bind(configuration.GetSection("Vnpay"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<VnpaySettings>>().Value);
        services.AddHostedService<VnpayStartupDiagnosticsService>();
        services.AddHttpClient<VnpayPaymentGateway>();
        return services;
    }
}
