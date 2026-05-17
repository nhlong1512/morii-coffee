using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Response body for <c>GET /api/v1/payments/by-order/{orderId}</c>.</summary>
public class OrderPaymentSummaryDto
{
    /// <summary>The order id this summary describes.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Latest payment status on the order — what the UI should display.</summary>
    public EPaymentStatus PaymentStatus { get; set; }

    /// <summary>All payment attempts against the order, newest first.</summary>
    public List<PaymentDto> Payments { get; set; } = new();
}
