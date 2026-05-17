using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Services.Payment;

/// <summary>
/// Emits one startup log line summarising the Stripe configuration mode. This keeps configuration
/// diagnostics out of service-registration code and avoids the <c>BuildServiceProvider()</c>
/// anti-pattern inside <c>IServiceCollection</c> extensions.
/// </summary>
public class StripeStartupDiagnosticsService : IHostedService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeStartupDiagnosticsService> _logger;

    public StripeStartupDiagnosticsService(
        StripeSettings settings,
        ILogger<StripeStartupDiagnosticsService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            _logger.LogWarning(
                "Stripe SecretKey is not configured. The /payments/* endpoints will fail until " +
                "Stripe__SecretKey is set.");
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Stripe configured in {Mode} mode. WebhookSigningSecret present: {WebhookConfigured}",
            _settings.IsLiveMode ? "LIVE" : "TEST",
            !string.IsNullOrWhiteSpace(_settings.WebhookSigningSecret));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
