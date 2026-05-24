using System.Security.Cryptography;
using System.Text;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Services.Shipping;

public class ShippingQuoteFingerprintService
{
    public string Generate(
        EDeliveryMethod deliveryMethod,
        EPaymentMethod paymentMethod,
        ShippingAddressDto address,
        ShippingPackageMetricsDto packageMetrics,
        int serviceId,
        int? serviceTypeId,
        DateTime quoteExpiresAtUtc)
    {
        var payload = string.Join('|',
            deliveryMethod,
            paymentMethod,
            address.FullName.Trim(),
            address.PhoneNumber.Trim(),
            address.AddressLine.Trim(),
            address.ProvinceId,
            address.DistrictId,
            address.WardCode?.Trim(),
            packageMetrics.TotalWeightGrams,
            packageMetrics.LengthCm,
            packageMetrics.WidthCm,
            packageMetrics.HeightCm,
            packageMetrics.ItemCount,
            serviceId,
            serviceTypeId?.ToString() ?? string.Empty,
            quoteExpiresAtUtc.ToUniversalTime().Ticks);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
