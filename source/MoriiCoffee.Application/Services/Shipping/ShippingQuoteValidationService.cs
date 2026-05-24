using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Services.Shipping;

public class ShippingQuoteValidationService
{
    private readonly ShippingQuoteFingerprintService _fingerprintService;

    public ShippingQuoteValidationService(ShippingQuoteFingerprintService fingerprintService)
    {
        _fingerprintService = fingerprintService;
    }

    public void EnsureValid(
        ShippingQuoteDto quote,
        EDeliveryMethod deliveryMethod,
        EPaymentMethod paymentMethod,
        ShippingAddressDto address,
        ShippingPackageMetricsDto packageMetrics)
    {
        ArgumentNullException.ThrowIfNull(quote);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(packageMetrics);

        if (quote.QuoteExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("The shipping quote has expired. Please refresh the quote and try again.");

        var utcExpiry = quote.QuoteExpiresAt.ToUniversalTime();
        var expirySeconds = new DateTime(utcExpiry.Year, utcExpiry.Month, utcExpiry.Day,
            utcExpiry.Hour, utcExpiry.Minute, utcExpiry.Second, DateTimeKind.Utc);

        var expected = _fingerprintService.Generate(
            deliveryMethod,
            paymentMethod,
            address,
            packageMetrics,
            quote.Service.ServiceId,
            quote.Service.ServiceTypeId,
            expirySeconds);

        if (!string.Equals(expected, quote.QuoteFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("The shipping quote is no longer valid for the current checkout data.");
    }
}
