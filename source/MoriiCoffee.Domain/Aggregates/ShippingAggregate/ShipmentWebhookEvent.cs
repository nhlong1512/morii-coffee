using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Domain.Aggregates.ShippingAggregate;

[Table("ShipmentWebhookEvents")]
public class ShipmentWebhookEvent : EntityBase
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    public EShippingProvider Provider { get; private set; }

    [MaxLength(100)]
    public string? ProviderEventId { get; private set; }

    [MaxLength(100)]
    public string? ProviderOrderCode { get; private set; }

    [MaxLength(100)]
    public string? ClientOrderCode { get; private set; }

    [Required]
    [MaxLength(200)]
    public string EventType { get; private set; } = null!;

    [Required]
    public string RawPayload { get; private set; } = null!;

    public bool SignatureVerified { get; private set; }

    [Required]
    [MaxLength(100)]
    public string ProcessingResult { get; private set; } = null!;

    public DateTime ReceivedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    private ShipmentWebhookEvent()
    {
    }

    public static ShipmentWebhookEvent Create(
        EShippingProvider provider,
        string eventType,
        string rawPayload,
        string? providerEventId = null,
        string? providerOrderCode = null,
        string? clientOrderCode = null,
        bool signatureVerified = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPayload);

        return new ShipmentWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ProviderEventId = string.IsNullOrWhiteSpace(providerEventId) ? null : providerEventId.Trim(),
            ProviderOrderCode = string.IsNullOrWhiteSpace(providerOrderCode) ? null : providerOrderCode.Trim(),
            ClientOrderCode = string.IsNullOrWhiteSpace(clientOrderCode) ? null : clientOrderCode.Trim(),
            EventType = eventType.Trim(),
            RawPayload = rawPayload,
            SignatureVerified = signatureVerified,
            ProcessingResult = "received",
            ReceivedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed(string processingResult, DateTime processedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processingResult);
        ProcessingResult = processingResult.Trim();
        ProcessedAt = processedAtUtc;
    }
}
