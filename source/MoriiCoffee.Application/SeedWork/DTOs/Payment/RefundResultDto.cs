using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Returned after a successful Stripe refund.</summary>
public class RefundResultDto
{
    [SwaggerSchema("Stripe Refund ID (re_...).")]
    public string RefundId { get; set; } = null!;

    [SwaggerSchema("Amount refunded.")]
    public decimal Amount { get; set; }

    [SwaggerSchema("Stripe refund status (e.g., 'succeeded').")]
    public string Status { get; set; } = null!;
}
