namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>
/// Represents the full cart state stored in Redis for a user.
/// </summary>
public class CartDto
{
    /// <summary>All items currently in the cart.</summary>
    public List<CartItemDto> Items { get; set; } = [];

    /// <summary>UTC timestamp when the cart was first created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last cart modification. Null if the cart has never been updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
