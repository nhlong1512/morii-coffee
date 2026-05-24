using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Dedicated abstraction over the GHN sandbox API.
/// The Application layer deals only with Morii-owned request/response models and never with raw GHN payloads.
/// </summary>
public interface IShippingGateway
{
    Task<IReadOnlyList<ShippingGatewayProvince>> GetProvincesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingGatewayDistrict>> GetDistrictsAsync(
        int provinceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingGatewayWard>> GetWardsAsync(
        int districtId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingGatewayService>> GetAvailableServicesAsync(
        ShippingGatewayAvailableServicesRequest request,
        CancellationToken cancellationToken = default);

    Task<ShippingGatewayFeeQuote> CalculateFeeAsync(
        ShippingGatewayFeeRequest request,
        CancellationToken cancellationToken = default);

    Task<ShippingGatewayLeadTimeQuote?> CalculateLeadTimeAsync(
        ShippingGatewayLeadTimeRequest request,
        CancellationToken cancellationToken = default);

    Task<ShippingGatewayCreateShipmentResult> CreateShipmentAsync(
        ShippingGatewayCreateShipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ShippingGatewayShipmentDetail> GetShipmentDetailAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default);

    Task<ShippingGatewayShipmentDetail> GetShipmentDetailByClientOrderCodeAsync(
        string clientOrderCode,
        CancellationToken cancellationToken = default);

    Task<ShippingGatewayCancelShipmentResult> CancelShipmentAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default);

    Task UpdateShipmentNoteAsync(
        string providerOrderCode,
        string note,
        CancellationToken cancellationToken = default);
}

public class ShippingGatewayAvailableServicesRequest
{
    public int ShopId { get; set; }

    public int FromDistrictId { get; set; }

    public int ToDistrictId { get; set; }
}

public class ShippingGatewayFeeRequest
{
    public int ShopId { get; set; }

    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = null!;

    public int ToDistrictId { get; set; }

    public string ToWardCode { get; set; } = null!;

    public int? ServiceId { get; set; }

    public int? ServiceTypeId { get; set; }

    public int LengthCm { get; set; }

    public int WidthCm { get; set; }

    public int HeightCm { get; set; }

    public int WeightGrams { get; set; }

    public decimal InsuranceValue { get; set; }

    public decimal CodAmount { get; set; }

    public IReadOnlyList<ShippingGatewayPackageItem> Items { get; set; } = [];
}

public class ShippingGatewayLeadTimeRequest
{
    public int FromDistrictId { get; set; }

    public string FromWardCode { get; set; } = null!;

    public int ToDistrictId { get; set; }

    public string ToWardCode { get; set; } = null!;

    public int ServiceId { get; set; }
}

public class ShippingGatewayPackageItem
{
    public string Name { get; set; } = null!;

    public int Quantity { get; set; }

    public int LengthCm { get; set; }

    public int WidthCm { get; set; }

    public int HeightCm { get; set; }

    public int WeightGrams { get; set; }
}

public class ShippingGatewayProvince
{
    public int ProvinceId { get; set; }

    public string ProvinceName { get; set; } = null!;

    public string? Code { get; set; }

    public bool IsActive { get; set; }
}

public class ShippingGatewayDistrict
{
    public int DistrictId { get; set; }

    public int ProvinceId { get; set; }

    public string DistrictName { get; set; } = null!;

    public int? SupportType { get; set; }

    public bool IsActive { get; set; }
}

public class ShippingGatewayWard
{
    public string WardCode { get; set; } = null!;

    public int DistrictId { get; set; }

    public string WardName { get; set; } = null!;

    public bool IsActive { get; set; }
}

public class ShippingGatewayService
{
    public int ServiceId { get; set; }

    public int? ServiceTypeId { get; set; }

    public string ShortName { get; set; } = null!;

    public string DisplayName { get; set; } = null!;
}

public class ShippingGatewayFeeQuote
{
    public decimal TotalFee { get; set; }

    public decimal ServiceFee { get; set; }

    public decimal InsuranceFee { get; set; }

    public decimal StationFee { get; set; }

    public decimal PickStationFee { get; set; }

    public decimal CouponValue { get; set; }

    public decimal R2SFee { get; set; }

    public decimal ReturnAgain { get; set; }

    public decimal DocumentReturn { get; set; }

    public decimal DoubleCheck { get; set; }

    public decimal CodFee { get; set; }

    public string RawPayload { get; set; } = null!;
}

public class ShippingGatewayLeadTimeQuote
{
    public DateTime? EstimatedDeliveryAtUtc { get; set; }

    public string RawPayload { get; set; } = null!;
}

public class ShippingGatewayCreateShipmentRequest
{
    public int ShopId { get; set; }

    public string ClientOrderCode { get; set; } = null!;

    public string ToName { get; set; } = null!;

    public string ToPhone { get; set; } = null!;

    public string ToAddress { get; set; } = null!;

    public string ToWardCode { get; set; } = null!;

    public int ToDistrictId { get; set; }

    public decimal CodAmount { get; set; }

    public string? Note { get; set; }

    public int ServiceId { get; set; }

    public int? ServiceTypeId { get; set; }

    public int WeightGrams { get; set; }

    public int LengthCm { get; set; }

    public int WidthCm { get; set; }

    public int HeightCm { get; set; }

    public decimal InsuranceValue { get; set; }

    public IReadOnlyList<ShippingGatewayCreateShipmentItem> Items { get; set; } = [];
}

public class ShippingGatewayCreateShipmentItem
{
    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public int Quantity { get; set; }

    public decimal? Price { get; set; }

    public int WeightGrams { get; set; }

    public int LengthCm { get; set; }

    public int WidthCm { get; set; }

    public int HeightCm { get; set; }
}

public class ShippingGatewayCreateShipmentResult
{
    public string ProviderOrderCode { get; set; } = null!;

    public string? ClientOrderCode { get; set; }

    public string Status { get; set; } = "created";

    public string StatusLabel { get; set; } = "created";

    public decimal? TotalFee { get; set; }

    public DateTime? ExpectedDeliveryAtUtc { get; set; }

    public string RawPayload { get; set; } = null!;
}

public class ShippingGatewayShipmentDetail
{
    public string ProviderOrderCode { get; set; } = null!;

    public string? ClientOrderCode { get; set; }

    public string Status { get; set; } = null!;

    public string StatusLabel { get; set; } = null!;

    public decimal? TotalFee { get; set; }

    public DateTime? ExpectedDeliveryAtUtc { get; set; }

    public string? ReasonCode { get; set; }

    public string? Reason { get; set; }

    public decimal? CodAmount { get; set; }

    public string RawPayload { get; set; } = null!;
}

public class ShippingGatewayCancelShipmentResult
{
    public string ProviderOrderCode { get; set; } = null!;

    public bool Success { get; set; }

    public string Message { get; set; } = null!;

    public string RawPayload { get; set; } = null!;
}
