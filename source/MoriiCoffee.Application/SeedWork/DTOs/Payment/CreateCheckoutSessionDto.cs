namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for <c>POST /api/v1/payments/checkout-session</c>.</summary>
public class CreateCheckoutSessionDto
{
    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Notes { get; set; }

    public bool SaveDeliveryProfile { get; set; }
}
