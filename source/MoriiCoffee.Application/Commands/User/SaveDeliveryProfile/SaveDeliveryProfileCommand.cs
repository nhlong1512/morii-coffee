using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.User.SaveDeliveryProfile;

/// <summary>Command to create or update the current user's default delivery profile.</summary>
public class SaveDeliveryProfileCommand : ICommand<DeliveryProfileDto>
{
    /// <summary>ID of the user whose profile is being saved.</summary>
    public Guid UserId { get; set; }

    /// <summary>Full name of the recipient.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Contact phone number for the delivery.</summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>Delivery address.</summary>
    public string Address { get; set; } = null!;

    public int? ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }
}
