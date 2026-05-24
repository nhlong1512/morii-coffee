using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;

namespace MoriiCoffee.Application.Services.Shipping;

/// <summary>
/// Computes Morii-owned parcel metrics from cart or order snapshots.
/// The current catalog has no per-product dimensions, so we use a conservative default heuristic.
/// </summary>
public class ShippingPackageMetricsService
{
    private const int DefaultUnitWeightGrams = 250;
    private const int DefaultLengthCm = 20;
    private const int DefaultWidthCm = 20;
    private const int BaseHeightCm = 8;

    public ShippingPackageMetricsDto BuildFromCart(IReadOnlyCollection<CartItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
            throw new InvalidOperationException("Cannot compute package metrics for an empty cart.");

        return Build(items.Select(item => (item.Quantity, item.UnitPrice)).ToList());
    }

    public ShippingPackageMetricsDto BuildFromOrder(IReadOnlyCollection<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
            throw new InvalidOperationException("Cannot compute package metrics for an empty order.");

        return Build(items.Select(item => (item.Quantity, item.UnitPrice)).ToList());
    }

    private static ShippingPackageMetricsDto Build(IReadOnlyCollection<(int Quantity, decimal UnitPrice)> items)
    {
        var totalQuantity = items.Sum(item => item.Quantity);
        var subtotal = items.Sum(item => item.UnitPrice * item.Quantity);

        return new ShippingPackageMetricsDto
        {
            TotalWeightGrams = Math.Max(DefaultUnitWeightGrams, totalQuantity * DefaultUnitWeightGrams),
            LengthCm = DefaultLengthCm,
            WidthCm = DefaultWidthCm,
            HeightCm = Math.Min(50, BaseHeightCm + (totalQuantity - 1) * 4),
            InsuranceValue = subtotal,
            ItemCount = totalQuantity
        };
    }
}
