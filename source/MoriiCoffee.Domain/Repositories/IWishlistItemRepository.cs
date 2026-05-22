using MoriiCoffee.Domain.Aggregates.WishlistAggregate;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing a user's wishlist items.
/// Each operation is scoped to a specific user.
/// </summary>
public interface IWishlistItemRepository
{
    /// <summary>Returns all wishlist items for <paramref name="userId"/>, ordered by AddedAt descending.</summary>
    Task<List<WishlistItem>> GetByUserIdAsync(Guid userId);

    /// <summary>Adds a product to the wishlist. No-op if already present.</summary>
    Task AddAsync(WishlistItem item);

    /// <summary>
    /// Returns true if the product is already in the wishlist.
    /// </summary>
    Task<bool> ExistsAsync(Guid userId, Guid productId);

    /// <summary>
    /// Removes the wishlist item for the given user and product.
    /// Returns false if the item was not found.
    /// </summary>
    Task<bool> RemoveAsync(Guid userId, Guid productId);

    /// <summary>Removes all wishlist items for the given user.</summary>
    Task ClearAsync(Guid userId);
}
