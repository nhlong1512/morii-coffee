using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;

/// <summary>
/// Audit / idempotency record for every Stripe webhook event received. The primary purpose is
/// idempotency: a UNIQUE constraint on <see cref="StripeEventId"/> turns a duplicate-delivery
/// race into a constraint-violation that the application interprets as
/// <see cref="EPaymentWebhookProcessingResult.Duplicate"/>.
/// </summary>
/// <remarks>
/// Although logically linked to the Payment aggregate, this row outlives Payment cleanup (the
/// FK uses <c>OnDelete(SetNull)</c>) because it doubles as a forensic trail for security and
/// incident investigation.
/// </remarks>
[Table("PaymentWebhookEvents")]
public class PaymentWebhookEvent : EntityBase
{
    private PaymentWebhookEvent()
    {
    }

    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; private set; }

    [Required]
    public EPaymentProvider Provider { get; private set; } = EPaymentProvider.Stripe;

    /// <summary>
    /// The Stripe event identifier (e.g. <c>evt_1Mw...</c>). UNIQUE — the database constraint is
    /// the source of truth for idempotency.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string StripeEventId { get; private set; } = null!;

    /// <summary>Stripe event type (e.g. <c>checkout.session.completed</c>).</summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string EventType { get; private set; } = null!;

    /// <summary>
    /// Hex-encoded SHA-256 of the raw request body. Lets us detect "same event id but different
    /// content" anomalies (which would be a sign of compromise) without persisting the entire
    /// payload (privacy + storage).
    /// </summary>
    [Required]
    [MaxLength(64)]
    [Column(TypeName = "varchar(64)")]
    public string PayloadFingerprint { get; private set; } = null!;

    /// <summary>
    /// <c>true</c> if the <c>Stripe-Signature</c> header verified against the configured signing secret.
    /// Always <c>true</c> for events that were dispatched into the application (we only persist
    /// after signature verification succeeds); a row with <c>false</c> exists only for forensic logging
    /// of attempted forgeries when explicitly recorded by an error path.
    /// </summary>
    [Required]
    public bool SignatureVerified { get; private set; }

    /// <summary>Final outcome of processing this event.</summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EPaymentWebhookProcessingResult ProcessingResult { get; private set; }

    /// <summary>
    /// Optional pointer to the local Payment row this event affected. Nullable so the audit row
    /// can outlive the Payment (e.g., if the Payment is later purged). FK has <c>OnDelete(SetNull)</c>.
    /// </summary>
    public Guid? RelatedPaymentId { get; private set; }

    /// <summary>Populated when <see cref="ProcessingResult"/> is <see cref="EPaymentWebhookProcessingResult.Failed"/>.</summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; private set; }

    /// <summary>UTC timestamp of when the event was first received by the API.</summary>
    [Required]
    public DateTime ReceivedAt { get; private set; }

    /// <summary>UTC timestamp of when processing finished (whatever the outcome).</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Factory used by the webhook handler at the moment the raw body is received and the signature
    /// has been verified. The row is intended to be inserted immediately to acquire the idempotency
    /// lock before any business state is changed.
    /// </summary>
    public static PaymentWebhookEvent Create(
        string stripeEventId,
        string eventType,
        string payloadFingerprint,
        bool signatureVerified,
        EPaymentProvider provider = EPaymentProvider.Stripe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadFingerprint);

        return new PaymentWebhookEvent
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            StripeEventId = stripeEventId.Trim(),
            EventType = eventType.Trim(),
            PayloadFingerprint = payloadFingerprint.Trim(),
            SignatureVerified = signatureVerified,
            ProcessingResult = EPaymentWebhookProcessingResult.Processed, // optimistic; updated by MarkProcessed
            ReceivedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update the row at the end of processing (called once per event, typically inside the same
    /// transaction that mutates Payment/Order state).
    /// </summary>
    public void MarkProcessed(
        EPaymentWebhookProcessingResult result,
        Guid? relatedPaymentId = null,
        string? errorMessage = null)
    {
        ProcessingResult = result;
        RelatedPaymentId = relatedPaymentId ?? RelatedPaymentId;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        ProcessedAt = DateTime.UtcNow;
    }
}
