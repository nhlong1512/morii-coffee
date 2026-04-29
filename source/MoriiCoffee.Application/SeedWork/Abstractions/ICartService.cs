using MoriiCoffee.Application.SeedWork.DTOs.Cart;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Manages the short-lived cart state stored in Redis for each authenticated user.
/// Cart data is persisted as a JSON payload keyed by user ID with a 24-hour TTL.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Returns the current cart for <paramref name="userId"/>.
    /// Never returns <c>null</c> — an empty <see cref="CartDto"/> is returned when no cart exists.
    /// </summary>
    Task<CartDto> GetCartAsync(Guid userId);

    /// <summary>
    /// Adds <paramref name="item"/> to the cart.
    /// If an entry with the same <c>ProductId</c> + <c>VariantId</c> already exists, its
    /// quantity is incremented instead of creating a duplicate.
    /// </summary>
    Task AddItemAsync(Guid userId, CartItemDto item);

    /// <summary>
    /// Removes the item matching <paramref name="productId"/> and <paramref name="variantId"/> from the cart.
    /// No-op when the item is not found.
    /// </summary>
    Task RemoveItemAsync(Guid userId, Guid productId, Guid? variantId);

    /// <summary>
    /// Updates the quantity of the item matching <paramref name="productId"/> and <paramref name="variantId"/>.
    /// Removes the item when <paramref name="quantity"/> is zero or negative.
    /// </summary>
    Task UpdateQuantityAsync(Guid userId, Guid productId, Guid? variantId, int quantity);

    /// <summary>Removes the entire cart for <paramref name="userId"/> from Redis.</summary>
    Task ClearCartAsync(Guid userId);

    /// <summary>
    /// Merges guest cart items into the authenticated user's cart.
    /// Items with the same <c>ProductId</c> + <c>VariantId</c> have their quantities summed;
    /// new items are appended.
    /// </summary>
    Task MergeAsync(Guid userId, List<CartItemDto> guestItems);
}
