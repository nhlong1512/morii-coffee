using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShippingQuoteDto
{
    public EShippingProvider Provider { get; set; }

    public string Environment { get; set; } = "sandbox";

    public ShippingAddressDto Address { get; set; } = new();

    public ShippingPackageMetricsDto PackageMetrics { get; set; } = new();

    public ShippingServiceOptionDto Service { get; set; } = new();

    public List<ShippingServiceOptionDto> AvailableServices { get; set; } = [];

    public ShippingFeeBreakdownDto FeeBreakdown { get; set; } = new();

    public DateTime? EstimatedDeliveryAt { get; set; }

    public DateTime QuoteExpiresAt { get; set; }

    public string QuoteFingerprint { get; set; } = null!;
}
