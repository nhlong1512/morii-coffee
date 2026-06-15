using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Commands.Payment.CreateVnpayPaymentUrl;

public sealed class CreateVnpayPaymentUrlCommand : ICommand<VnpayPaymentUrlResponseDto>
{
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = "127.0.0.1";
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public int? ProvinceId { get; set; }
    public string? ProvinceName { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string? WardCode { get; set; }
    public string? WardName { get; set; }
    public string? Notes { get; set; }
    public bool SaveDeliveryProfile { get; set; }
    public EDeliveryMethod DeliveryMethod { get; set; } = EDeliveryMethod.GHN_DELIVERY;
    public string? ShippingQuoteFingerprint { get; set; }
    public int? ShippingServiceId { get; set; }
    public int? ShippingServiceTypeId { get; set; }
    public string? ShippingServiceLabel { get; set; }
    public decimal? ShippingFee { get; set; }
    public DateTime? ShippingQuoteExpiresAt { get; set; }
    public string? ShippingProviderEnvironment { get; set; }
}
