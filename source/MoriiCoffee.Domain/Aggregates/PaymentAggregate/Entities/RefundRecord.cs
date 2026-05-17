using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;

/// <summary>
/// One refund issued against a successful <see cref="Payment"/>. A <see cref="Payment"/> may have
/// multiple <see cref="RefundRecord"/> children (partial refunds summing up to the original amount).
/// </summary>
/// <remarks>
/// Refunds are asynchronous at Stripe: the local row is created with <see cref="ERefundStatus.Pending"/>
/// when the Stripe API accepts the refund, and is flipped to <see cref="ERefundStatus.Succeeded"/>
/// only when the <c>charge.refunded</c> webhook is processed.
/// </remarks>
[Table("Refunds")]
public class RefundRecord : EntityBase
{
    // EF Core requires a parameterless ctor to materialise rows from the database. Kept private so
    // domain code must use the static factory <see cref="Create"/>.
    private RefundRecord()
    {
    }

    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; private set; }

    /// <summary>FK to the parent <see cref="Payment"/> row.</summary>
    [Required]
    public Guid PaymentId { get; private set; }

    /// <summary>
    /// Stripe Refund identifier (e.g. <c>re_3OZB...</c>). Stored to correlate the <c>charge.refunded</c>
    /// webhook event back to this local row. UNIQUE across the table.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string StripeRefundId { get; private set; } = null!;

    /// <summary>
    /// Refunded amount in VND (zero-decimal currency). Equals the integer the Stripe API echoes back.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; private set; }

    /// <summary>Free-text reason recorded by the admin who issued the refund.</summary>
    [MaxLength(500)]
    public string? Reason { get; private set; }

    /// <summary>Current settlement status of the refund (Pending → Succeeded / Failed).</summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ERefundStatus Status { get; private set; }

    /// <summary>
    /// Id of the admin user (AspNetUsers.Id) who initiated the refund. Recorded for audit.
    /// FK is configured in <c>RefundRecordConfiguration</c> with <c>OnDelete(Restrict)</c>.
    /// </summary>
    [Required]
    public Guid InitiatedByAdminUserId { get; private set; }

    /// <summary>
    /// Factory used by the application layer when the Stripe API has accepted a refund. The local
    /// row is created in <see cref="ERefundStatus.Pending"/>; the <c>charge.refunded</c> webhook
    /// flips it later.
    /// </summary>
    /// <param name="paymentId">Owning <see cref="Payment"/> id.</param>
    /// <param name="stripeRefundId">Stripe refund id returned from <c>RefundService.CreateAsync</c>.</param>
    /// <param name="amount">Refund amount in VND. Must be greater than zero.</param>
    /// <param name="initiatedByAdminUserId">Id of the admin issuing the refund.</param>
    /// <param name="reason">Optional free-text reason.</param>
    public static RefundRecord Create(
        Guid paymentId,
        string stripeRefundId,
        decimal amount,
        Guid initiatedByAdminUserId,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeRefundId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        return new RefundRecord
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            StripeRefundId = stripeRefundId.Trim(),
            Amount = amount,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Status = ERefundStatus.Pending,
            InitiatedByAdminUserId = initiatedByAdminUserId
        };
    }

    /// <summary>
    /// Flips the refund to <see cref="ERefundStatus.Succeeded"/> after the <c>charge.refunded</c>
    /// webhook is verified. Idempotent: a second call with the row already in <c>Succeeded</c>
    /// is a no-op so Stripe retries cannot double-flip state.
    /// </summary>
    public void MarkSucceeded()
    {
        if (Status == ERefundStatus.Succeeded)
            return;

        if (Status != ERefundStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot transition refund from {Status} to Succeeded.");

        Status = ERefundStatus.Succeeded;
    }

    /// <summary>
    /// Flips the refund to <see cref="ERefundStatus.Failed"/>. Idempotent.
    /// </summary>
    /// <param name="reason">Optional reason returned by Stripe; appended to the existing reason note.</param>
    public void MarkFailed(string? reason)
    {
        if (Status == ERefundStatus.Failed)
            return;

        if (Status != ERefundStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot transition refund from {Status} to Failed.");

        Status = ERefundStatus.Failed;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = string.IsNullOrWhiteSpace(Reason)
                ? reason.Trim()
                : $"{Reason} | failed: {reason.Trim()}";
        }
    }
}
