using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShipmentSummaryDto
{
    public Guid Id { get; set; }

    public EShippingProvider Provider { get; set; }

    public string ProviderEnvironment { get; set; } = "sandbox";

    public EShipmentStatus Status { get; set; }

    public string StatusLabel { get; set; } = null!;

    public string ClientOrderCode { get; set; } = null!;

    public string? ProviderOrderCode { get; set; }

    public int? ShopId { get; set; }

    public int? ServiceId { get; set; }

    public int? ServiceTypeId { get; set; }

    public decimal? FeeTotal { get; set; }

    public DateTime? ExpectedDeliveryAt { get; set; }

    public string? TrackingUrl { get; set; }

    public string? FailureReasonCode { get; set; }

    public string? FailureReason { get; set; }

    public string? Note { get; set; }

    public DateTime? LastSyncedAt { get; set; }
}
