using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Payment;

namespace MoriiCoffee.Domain.Aggregates.PaymentAggregate;

/// <summary>
/// Represents a Stripe payment intent and its lifecycle.
/// Created when a customer initiates checkout; updated by the Stripe webhook handler
/// as the payment progresses through pending → succeeded/failed → refunded.
/// </summary>
[Table("Payments")]
public class Payment : AggregateRoot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>ID of the user who initiated this payment.</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Payment amount in the smallest currency unit (e.g., cents for USD, VND for VND).</summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code (e.g., "vnd", "usd"). Stored lowercase.</summary>
    [Required]
    [MaxLength(10)]
    [Column(TypeName = "nvarchar(10)")]
    public string Currency { get; set; } = "vnd";

    /// <summary>Stripe PaymentIntent ID (e.g., "pi_3Xxx..."). Used to correlate with Stripe events.</summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string StripePaymentIntentId { get; set; } = null!;

    /// <summary>Current lifecycle status of the payment.</summary>
    public EPaymentStatus Status { get; set; } = EPaymentStatus.Pending;

    /// <summary>Payment method type used (e.g., "card", "bank_transfer"). Set after confirmation.</summary>
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? PaymentMethod { get; set; }

    /// <summary>Human-readable description of what the payment is for.</summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    #region Domain Methods

    /// <summary>Transitions the payment to Succeeded after confirmation from Stripe.</summary>
    public void MarkSucceeded(string? paymentMethod = null)
    {
        Status = EPaymentStatus.Succeeded;
        PaymentMethod = paymentMethod;
    }

    /// <summary>Transitions the payment to Failed when Stripe reports a failure.</summary>
    public void MarkFailed() => Status = EPaymentStatus.Failed;

    /// <summary>Transitions the payment to Canceled.</summary>
    public void MarkCanceled() => Status = EPaymentStatus.Canceled;

    /// <summary>Transitions the payment to Refunded after a successful refund on Stripe.</summary>
    public void MarkRefunded() => Status = EPaymentStatus.Refunded;

    #endregion
}
