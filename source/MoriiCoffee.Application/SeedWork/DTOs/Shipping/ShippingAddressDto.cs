namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

/// <summary>
/// Structured delivery address used by checkout, saved delivery profiles, and order snapshots.
/// </summary>
public class ShippingAddressDto
{
    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string AddressLine { get; set; } = null!;

    public int? ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }
}
