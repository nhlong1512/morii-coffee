namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShippingProvinceDto
{
    public int ProvinceId { get; set; }

    public string ProvinceName { get; set; } = null!;

    public string? Code { get; set; }
}
