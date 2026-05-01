using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Order;

/// <summary>
/// Lightweight order summary used in list responses.
/// Omits item details to keep payloads small.
/// </summary>
public class OrderSummaryDto
{
    /// <summary>Unique identifier of the order.</summary>
    public Guid Id { get; set; }

    /// <summary>Human-readable order number (e.g., "MRC-20260430-001").</summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>Final total amount payable for the order.</summary>
    public decimal Total { get; set; }

    /// <summary>Current lifecycle status of the order.</summary>
    public EOrderStatus OrderStatus { get; set; }

    /// <summary>Payment method selected when the order was placed.</summary>
    public EPaymentMethod PaymentMethod { get; set; }

    /// <summary>UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update, or null if unchanged since creation.</summary>
    public DateTime? UpdatedAt { get; set; }
}
