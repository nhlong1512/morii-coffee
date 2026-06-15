using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// One refund against a Payment. Doubles as the response body for the refund creation endpoint
/// (admin), and as a child item inside <see cref="PaymentDto.Refunds"/>.
/// </summary>
public class RefundDto
{
    public EPaymentProvider Provider { get; set; }

    public string ProviderRefundId => StripeRefundId;
    /// <summary>Internal refund record id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stripe Refund id (e.g. <c>re_3OZB...</c>).</summary>
    public string StripeRefundId { get; set; } = null!;

    /// <summary>Refund amount in VND.</summary>
    public decimal Amount { get; set; }

    /// <summary>Optional admin-supplied reason.</summary>
    public string? Reason { get; set; }

    /// <summary>Pending / Succeeded / Failed.</summary>
    public ERefundStatus Status { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}
