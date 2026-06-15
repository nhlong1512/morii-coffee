using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Services.Payment;

public sealed class VnpayStartupDiagnosticsService : IHostedService
{
    private readonly VnpaySettings _settings;
    private readonly ILogger<VnpayStartupDiagnosticsService> _logger;

    public VnpayStartupDiagnosticsService(VnpaySettings settings, ILogger<VnpayStartupDiagnosticsService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.TmnCode) || string.IsNullOrWhiteSpace(_settings.HashSecret))
            _logger.LogWarning("VNPAY is not configured. Set Vnpay__TmnCode and Vnpay__HashSecret.");
        else
            _logger.LogInformation("VNPAY configured for endpoint {Endpoint}; refunds enabled: {RefundEnabled}",
                _settings.PaymentUrl, _settings.RefundEnabled);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
