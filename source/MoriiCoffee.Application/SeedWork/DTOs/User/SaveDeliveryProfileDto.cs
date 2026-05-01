namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Request body for creating or updating the current user's delivery profile.</summary>
public class SaveDeliveryProfileDto
{
    /// <summary>Full name of the recipient.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Contact phone number for the delivery.</summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>Delivery address.</summary>
    public string Address { get; set; } = null!;
}
