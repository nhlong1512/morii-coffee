using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Domain.Aggregates.ShippingAggregate;

[Table("Shipments")]
public class Shipment : EntityBase
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    public Guid OrderId { get; private set; }

    [Required]
    public EShippingProvider Provider { get; private set; }

    [Required]
    [MaxLength(50)]
    public string ProviderEnvironment { get; private set; } = "sandbox";

    [Required]
    public EShipmentStatus Status { get; private set; }

    [Required]
    [MaxLength(200)]
    public string StatusLabel { get; private set; } = null!;

    [Required]
    [MaxLength(100)]
    public string ClientOrderCode { get; private set; } = null!;

    [MaxLength(100)]
    public string? ProviderOrderCode { get; private set; }

    public int? ShopId { get; private set; }

    public int? ServiceId { get; private set; }

    public int? ServiceTypeId { get; private set; }

    public decimal CodAmount { get; private set; }

    public decimal? FeeTotal { get; private set; }

    public DateTime? ExpectedDeliveryAt { get; private set; }

    [MaxLength(500)]
    public string? TrackingUrl { get; private set; }

    [MaxLength(100)]
    public string? FailureReasonCode { get; private set; }

    [MaxLength(1000)]
    public string? FailureReason { get; private set; }

    [MaxLength(1000)]
    public string? Note { get; private set; }

    public string? LastRawDetailPayload { get; private set; }

    public DateTime? LastSyncedAt { get; private set; }

    private Shipment()
    {
    }

    public static Shipment CreatePending(
        Guid orderId,
        string clientOrderCode,
        string providerEnvironment,
        decimal codAmount,
        int? shopId,
        int? serviceId,
        int? serviceTypeId)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId must not be empty.", nameof(orderId));

        ArgumentException.ThrowIfNullOrWhiteSpace(clientOrderCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEnvironment);
        ArgumentOutOfRangeException.ThrowIfNegative(codAmount);

        return new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Provider = EShippingProvider.GHN,
            ProviderEnvironment = providerEnvironment.Trim(),
            Status = EShipmentStatus.CREATE_PENDING,
            StatusLabel = EShipmentStatus.CREATE_PENDING.ToString(),
            ClientOrderCode = clientOrderCode.Trim(),
            CodAmount = codAmount,
            ShopId = shopId,
            ServiceId = serviceId,
            ServiceTypeId = serviceTypeId
        };
    }

    public void MarkCreated(
        string providerOrderCode,
        string statusLabel,
        decimal? feeTotal,
        DateTime? expectedDeliveryAt,
        string? trackingUrl,
        string? rawPayload,
        DateTime syncedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(statusLabel);

        ProviderOrderCode = providerOrderCode.Trim();
        Status = EShipmentStatus.CREATED;
        StatusLabel = statusLabel.Trim();
        FeeTotal = feeTotal;
        ExpectedDeliveryAt = expectedDeliveryAt;
        TrackingUrl = string.IsNullOrWhiteSpace(trackingUrl) ? null : trackingUrl.Trim();
        LastRawDetailPayload = string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload;
        LastSyncedAt = syncedAtUtc;
        FailureReasonCode = null;
        FailureReason = null;
    }

    public void MarkCreateFailed(string? failureReasonCode, string failureReason, DateTime syncedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        Status = EShipmentStatus.FAILED_TO_CREATE;
        StatusLabel = EShipmentStatus.FAILED_TO_CREATE.ToString();
        FailureReasonCode = string.IsNullOrWhiteSpace(failureReasonCode) ? null : failureReasonCode.Trim();
        FailureReason = failureReason.Trim();
        LastSyncedAt = syncedAtUtc;
    }

    public void ApplyProviderUpdate(
        EShipmentStatus status,
        string statusLabel,
        string? providerOrderCode,
        decimal? feeTotal,
        DateTime? expectedDeliveryAt,
        string? trackingUrl,
        string? rawPayload,
        string? failureReasonCode,
        string? failureReason,
        DateTime syncedAtUtc)
    {
        Status = status;
        StatusLabel = string.IsNullOrWhiteSpace(statusLabel) ? status.ToString() : statusLabel.Trim();
        ProviderOrderCode = string.IsNullOrWhiteSpace(providerOrderCode) ? ProviderOrderCode : providerOrderCode.Trim();
        FeeTotal = feeTotal ?? FeeTotal;
        ExpectedDeliveryAt = expectedDeliveryAt ?? ExpectedDeliveryAt;
        TrackingUrl = string.IsNullOrWhiteSpace(trackingUrl) ? TrackingUrl : trackingUrl.Trim();
        LastRawDetailPayload = string.IsNullOrWhiteSpace(rawPayload) ? LastRawDetailPayload : rawPayload;
        FailureReasonCode = string.IsNullOrWhiteSpace(failureReasonCode) ? null : failureReasonCode.Trim();
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        LastSyncedAt = syncedAtUtc;
    }

    public void UpdateNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
