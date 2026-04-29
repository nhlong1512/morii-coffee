using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Infrastructure.Services.Cart;

/// <summary>
/// Redis-backed implementation of <see cref="ICartService"/>.
/// Cart state is stored as a serialized <see cref="CartDto"/> with a 24-hour TTL.
/// Uses <see cref="ICacheService"/> to reuse the existing serialization and logging layer.
/// </summary>
public class RedisCartService : ICartService
{
    private readonly ICacheService _cache;

    public RedisCartService(ICacheService cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        return await _cache.GetDataAsync<CartDto>(CachedKeyConstants.CartByUser(userId))
               ?? new CartDto { CreatedAt = DateTime.UtcNow };
    }

    /// <inheritdoc />
    public async Task AddItemAsync(Guid userId, CartItemDto item)
    {
        var cart = await GetCartAsync(userId);

        var existing = FindItem(cart, item.ProductId, item.VariantId);
        if (existing is not null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            item.AddedAt = DateTime.UtcNow;
            cart.Items.Add(item);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(Guid userId, Guid productId, Guid? variantId)
    {
        var cart = await GetCartAsync(userId);

        var item = FindItem(cart, productId, variantId);
        if (item is null) return;

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
    }

    /// <inheritdoc />
    public async Task UpdateQuantityAsync(Guid userId, Guid productId, Guid? variantId, int quantity)
    {
        var cart = await GetCartAsync(userId);

        var item = FindItem(cart, productId, variantId);
        if (item is null) return;

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
    }

    /// <inheritdoc />
    public async Task ClearCartAsync(Guid userId)
    {
        await _cache.RemoveDataAsync(CachedKeyConstants.CartByUser(userId));
    }

    /// <inheritdoc />
    public async Task MergeAsync(Guid userId, List<CartItemDto> guestItems)
    {
        if (guestItems.Count == 0) return;

        var cart = await GetCartAsync(userId);

        foreach (var guestItem in guestItems)
        {
            var existing = FindItem(cart, guestItem.ProductId, guestItem.VariantId);
            if (existing is not null)
            {
                existing.Quantity += guestItem.Quantity;
            }
            else
            {
                guestItem.AddedAt = DateTime.UtcNow;
                cart.Items.Add(guestItem);
            }
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
    }

    private static CartItemDto? FindItem(CartDto cart, Guid productId, Guid? variantId)
    {
        return cart.Items.FirstOrDefault(i =>
            i.ProductId == productId && i.VariantId == variantId);
    }

    private async Task SaveCartAsync(Guid userId, CartDto cart)
    {
        await _cache.SetDataAsync(
            CachedKeyConstants.CartByUser(userId),
            cart,
            CacheTtlConstants.Cart);
    }
}
