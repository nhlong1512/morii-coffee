namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class ShippingFeeBreakdownDto
{
    public decimal TotalFee { get; set; }

    public decimal ServiceFee { get; set; }

    public decimal InsuranceFee { get; set; }

    public decimal StationFee { get; set; }

    public decimal PickStationFee { get; set; }

    public decimal CouponValue { get; set; }

    public decimal R2SFee { get; set; }

    public decimal ReturnAgainFee { get; set; }

    public decimal DocumentReturnFee { get; set; }

    public decimal DoubleCheckFee { get; set; }

    public decimal CodFee { get; set; }

    public string? RawPayload { get; set; }
}
