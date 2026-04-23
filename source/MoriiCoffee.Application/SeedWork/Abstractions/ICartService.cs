using MoriiCoffee.Application.SeedWork.DTOs.Cart;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Redis-backed cart for authenticated users.
/// One cart document per user; TTL refreshes on every successful mutation.
/// Throws <see cref="ServiceUnavailableException"/> when Redis is unreachable.
/// </summary>
public interface ICartService
{
    /// <summary>Returns the user's active cart, or an empty cart document if none exists.</summary>
    Task<CartDto> GetCartAsync(Guid userId);

    /// <summary>
    /// Adds a variant to the cart. If the variant is already in the cart, quantity accumulates.
    /// The <paramref name="snapshot"/> carries product/variant display data captured at add time.
    /// </summary>
    Task<CartDto> AddItemAsync(Guid userId, CartItemDto snapshot);

    /// <summary>
    /// Sets the quantity of an existing cart line.
    /// Quantity 0 removes the line. Throws NotFoundException if the line does not exist.
    /// </summary>
    Task<CartDto> UpdateItemQuantityAsync(Guid userId, Guid variantId, int quantity);

    /// <summary>Removes a variant line from the cart. Throws NotFoundException if not present.</summary>
    Task<CartDto> RemoveItemAsync(Guid userId, Guid variantId);

    /// <summary>Deletes the user's cart document from Redis.</summary>
    Task ClearCartAsync(Guid userId);
}
