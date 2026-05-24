namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShippingServiceOptionDto
{
    public int ServiceId { get; set; }

    public int? ServiceTypeId { get; set; }

    public string ShortName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public DateTime? EstimatedLeadTime { get; set; }

    public decimal? Fee { get; set; }

    public bool IsRecommended { get; set; }
}
