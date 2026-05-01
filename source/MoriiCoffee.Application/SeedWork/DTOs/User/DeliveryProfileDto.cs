namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>
/// Read model returned by the delivery-profile endpoints.
/// Contains the user's saved default delivery information.
/// </summary>
public class DeliveryProfileDto
{
    /// <summary>Full name of the recipient.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Contact phone number for the delivery.</summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>Delivery address.</summary>
    public string Address { get; set; } = null!;
}
