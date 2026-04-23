namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>
/// Represents one authenticated customer's active cart stored in Redis.
/// The document is rewritten on every successful mutation and the TTL is refreshed.
/// </summary>
public class CartDto
{
    /// <summary>Authenticated user who owns this cart.</summary>
    public Guid UserId { get; set; }

    /// <summary>Selected product variant lines.</summary>
    public List<CartItemDto> Items { get; set; } = new();

    /// <summary>Sum of all line totals.</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>UTC timestamp of the last successful cart mutation.</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the cart will expire if inactive.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}
