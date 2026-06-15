using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Domain.Aggregates.PaymentAggregate;

/// <summary>
/// Represents one Stripe Checkout Session against an <c>Order</c>. Multiple Payment rows can
/// exist for a single Order — e.g. a failed first attempt followed by a successful retry. The
/// Order aggregate stores the latest <see cref="EPaymentStatus"/>; this aggregate stores the
/// full per-attempt audit.
/// </summary>
/// <remarks>
/// <para>Encapsulates the full lifecycle of one payment attempt: Created → (Succeeded | Failed | Expired).</para>
/// <para>Owns <see cref="RefundRecord"/> children — refunds are scoped to a successful Payment.</para>
/// </remarks>
[Table("Payments")]
public class Payment : AggregateRoot
{
    private readonly List<RefundRecord> _refunds = new();

    private Payment()
    {
    }

    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; private set; }

    /// <summary>FK to the <c>Order</c> this payment attempt belongs to.</summary>
    [Required]
    public Guid OrderId { get; private set; }

    [Required]
    public EPaymentProvider Provider { get; private set; } = EPaymentProvider.Stripe;

    /// <summary>
    /// Stripe Checkout Session id (e.g. <c>cs_test_...</c>). UNIQUE across the table — the webhook
    /// handler uses this as the natural key to find a Payment from an incoming event.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string StripeSessionId { get; private set; } = null!;

    /// <summary>
    /// Stripe PaymentIntent id (e.g. <c>pi_3OZA...</c>). Populated when the session is completed.
    /// Null while the session is in <see cref="EPaymentTransactionStatus.Created"/>.
    /// </summary>
    [MaxLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string? StripePaymentIntentId { get; private set; }

    /// <summary>Stripe Charge id (<c>ch_...</c>). Populated once the charge has been captured.</summary>
    [MaxLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string? StripeChargeId { get; private set; }

    /// <summary>Charged amount in the order's currency (VND uses zero-decimal: integer == đồng).</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; private set; }

    /// <summary>ISO 4217 currency code (e.g. <c>vnd</c>).</summary>
    [Required]
    [MaxLength(3)]
    [Column(TypeName = "varchar(3)")]
    public string Currency { get; private set; } = null!;

    /// <summary>Lifecycle status of this Payment row.</summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EPaymentTransactionStatus Status { get; private set; }

    /// <summary>Free-text failure reason populated on <see cref="EPaymentTransactionStatus.Failed"/>.</summary>
    [MaxLength(500)]
    public string? FailureReason { get; private set; }

    /// <summary>Read-only view of the refund history for this payment.</summary>
    public IReadOnlyCollection<RefundRecord> Refunds => _refunds.AsReadOnly();

    /// <summary>
    /// Factory: creates a new Payment row in <see cref="EPaymentTransactionStatus.Created"/>
    /// immediately after the Stripe Checkout Session is created.
    /// </summary>
    /// <param name="orderId">Owning Order id.</param>
    /// <param name="stripeSessionId">Session id returned by <c>SessionService.CreateAsync</c>.</param>
    /// <param name="amount">Amount in VND (must be greater than zero and match the order total).</param>
    /// <param name="currency">ISO 4217 lowercase (e.g. <c>vnd</c>).</param>
    public static Payment Create(
        Guid orderId,
        string stripeSessionId,
        decimal amount,
        string currency,
        Guid? id = null,
        EPaymentProvider provider = EPaymentProvider.Stripe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        return new Payment
        {
            Id = id ?? Guid.NewGuid(),
            OrderId = orderId,
            Provider = provider,
            StripeSessionId = stripeSessionId.Trim(),
            Amount = amount,
            Currency = currency.Trim().ToLowerInvariant(),
            Status = EPaymentTransactionStatus.Created
        };
    }

    /// <summary>
    /// Marks this Payment as <see cref="EPaymentTransactionStatus.Succeeded"/> after the
    /// <c>checkout.session.completed</c> webhook is verified. Idempotent: if already succeeded
    /// with the same <paramref name="paymentIntentId"/>, this is a no-op.
    /// </summary>
    public void MarkSucceeded(string paymentIntentId, string chargeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chargeId);

        if (Status == EPaymentTransactionStatus.Succeeded)
        {
            // Idempotent: same event delivered twice. Confirm identity to detect mismatched data.
            if (!string.Equals(StripePaymentIntentId, paymentIntentId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Payment is already Succeeded with a different PaymentIntent id.");
            return;
        }

        if (Status != EPaymentTransactionStatus.Created)
            throw new InvalidOperationException(
                $"Cannot transition payment from {Status} to Succeeded.");

        Status = EPaymentTransactionStatus.Succeeded;
        StripePaymentIntentId = paymentIntentId.Trim();
        StripeChargeId = chargeId.Trim();
    }

    /// <summary>Marks this Payment as <see cref="EPaymentTransactionStatus.Failed"/>. Idempotent.</summary>
    public void MarkFailed(string? reason)
    {
        if (Status == EPaymentTransactionStatus.Failed)
            return;

        if (Status != EPaymentTransactionStatus.Created)
            throw new InvalidOperationException(
                $"Cannot transition payment from {Status} to Failed.");

        Status = EPaymentTransactionStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>Marks this Payment as <see cref="EPaymentTransactionStatus.Expired"/>. Idempotent.</summary>
    public void MarkExpired()
    {
        if (Status == EPaymentTransactionStatus.Expired)
            return;

        if (Status != EPaymentTransactionStatus.Created)
            throw new InvalidOperationException(
                $"Cannot transition payment from {Status} to Expired.");

        Status = EPaymentTransactionStatus.Expired;
    }

    /// <summary>
    /// Attach a refund record. Enforces: status must be Succeeded; sum of refunds cannot exceed Amount.
    /// Called by the application layer after Stripe's <c>RefundService.CreateAsync</c> returns success.
    /// </summary>
    public void AddRefund(RefundRecord refund)
    {
        ArgumentNullException.ThrowIfNull(refund);

        if (Status != EPaymentTransactionStatus.Succeeded)
            throw new InvalidOperationException("Refunds may only be attached to a succeeded payment.");

        if (refund.PaymentId != Id)
            throw new InvalidOperationException("Refund does not belong to this payment.");

        // Count both Pending and Succeeded refunds against the cap — a Pending refund is already
        // accepted by Stripe and reserves part of the balance until charge.refunded confirms it.
        var alreadyReserved = _refunds
            .Where(r => r.Status != ERefundStatus.Failed)
            .Sum(r => r.Amount);

        if (alreadyReserved + refund.Amount > Amount)
            throw new InvalidOperationException(
                "Refund amount exceeds the remaining unrefunded balance on this payment.");

        _refunds.Add(refund);
    }

    /// <summary>
    /// Sum of all settled (<see cref="ERefundStatus.Succeeded"/>) refunds. Used by the Order
    /// aggregate to decide whether the order's payment status should be <c>Refunded</c> or
    /// <c>PartiallyRefunded</c>.
    /// </summary>
    public decimal TotalSucceededRefundAmount =>
        _refunds.Where(r => r.Status == ERefundStatus.Succeeded).Sum(r => r.Amount);
}
