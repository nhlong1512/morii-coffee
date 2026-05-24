namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShippingDistrictDto
{
    public int DistrictId { get; set; }

    public int ProvinceId { get; set; }

    public string DistrictName { get; set; } = null!;

    public int? SupportType { get; set; }
}
