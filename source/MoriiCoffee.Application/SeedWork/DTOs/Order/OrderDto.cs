using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;

namespace MoriiCoffee.Application.SeedWork.DTOs.Order;

/// <summary>
/// Full order response including all items and delivery details.
/// Returned after placing an order or fetching a specific order by ID.
/// </summary>
public class OrderDto
{
    /// <summary>Unique identifier of the order.</summary>
    public Guid Id { get; set; }

    /// <summary>Human-readable order number (e.g., "MRC-20260430-001").</summary>
    public string OrderNumber { get; set; } = null!;

    /// <summary>Identifier of the user who placed the order.</summary>
    public Guid UserId { get; set; }

    /// <summary>Recipient's full name for delivery.</summary>
    public string DeliveryFullName { get; set; } = null!;

    /// <summary>Recipient's contact phone number.</summary>
    public string DeliveryPhoneNumber { get; set; } = null!;

    /// <summary>Full street address for delivery.</summary>
    public string DeliveryAddress { get; set; } = null!;

    /// <summary>Optional order notes provided by the customer.</summary>
    public string? Notes { get; set; }

    /// <summary>Payment method selected when the order was placed.</summary>
    public EPaymentMethod PaymentMethod { get; set; }

    /// <summary>Sum of all line item totals before tax, shipping, and discount.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Tax amount applied to the order.</summary>
    public decimal Tax { get; set; }

    /// <summary>Shipping fee applied to the order.</summary>
    public decimal Shipping { get; set; }

    /// <summary>Discount amount deducted from the order total.</summary>
    public decimal Discount { get; set; }

    /// <summary>Final amount payable: <c>Subtotal + Tax + Shipping - Discount</c>.</summary>
    public decimal Total { get; set; }

    /// <summary>Current lifecycle status of the order.</summary>
    public EOrderStatus OrderStatus { get; set; }

    /// <summary>Payment-related state and latest Stripe attempt details for this order.</summary>
    public OrderPaymentInfoDto PaymentInfo { get; set; } = new();

    /// <summary>All line items belonging to this order.</summary>
    public List<OrderItemDto> Items { get; set; } = [];

    /// <summary>UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the most recent status update. Null if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
