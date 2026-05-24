namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShippingPackageMetricsDto
{
    public int TotalWeightGrams { get; set; }

    public int LengthCm { get; set; }

    public int WidthCm { get; set; }

    public int HeightCm { get; set; }

    public decimal InsuranceValue { get; set; }

    public int ItemCount { get; set; }
}
