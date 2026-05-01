using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Order;

/// <summary>
/// Request payload sent by the client to place a new order from the current cart.
/// </summary>
public class PlaceOrderDto
{
    /// <summary>Recipient's full name for delivery.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Recipient's contact phone number.</summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>Full street address for delivery.</summary>
    public string Address { get; set; } = null!;

    /// <summary>Optional order notes (e.g., "leave at front door").</summary>
    public string? Notes { get; set; }

    /// <summary>Payment method chosen by the customer (e.g., COD, MOMO, PAYPAL).</summary>
    public EPaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// When <c>true</c>, the provided delivery info is saved as the user's default delivery profile.
    /// </summary>
    public bool SaveDeliveryProfile { get; set; }
}
