namespace MoriiCoffee.Application.SeedWork.DTOs.Order;

/// <summary>
/// Represents a single line item within an order response.
/// All fields are a snapshot captured at order placement time.
/// </summary>
public class OrderItemDto
{
    /// <summary>Unique identifier of the order item.</summary>
    public Guid Id { get; set; }

    /// <summary>Identifier of the original product.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Product display name at the time of order placement.</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Optional variant identifier (e.g., a specific size). Null when no variant was selected.</summary>
    public Guid? VariantId { get; set; }

    /// <summary>Human-readable variant label (e.g., "Size M"). Null when no variant.</summary>
    public string? VariantLabel { get; set; }

    /// <summary>Current thumbnail image URL for the product.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Unit price at the time of order placement (VND).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Number of units ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>Total price for this line item (<c>UnitPrice × Quantity</c>).</summary>
    public decimal LineTotal { get; set; }
}
