using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Order;

/// <summary>
/// Request payload for updating an order's lifecycle status (admin-only operation).
/// </summary>
public class UpdateOrderStatusDto
{
    /// <summary>The target status to transition the order into.</summary>
    public EOrderStatus NewStatus { get; set; }
}
