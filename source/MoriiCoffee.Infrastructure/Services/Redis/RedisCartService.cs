using System.Text.Json;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Settings;
using StackExchange.Redis;

namespace MoriiCoffee.Infrastructure.Services.Redis;

/// <summary>
/// Redis-backed cart service for authenticated users.
/// Stores one JSON cart document per user keyed as <c>cart:{userId}</c>.
/// TTL is refreshed on every successful mutation.
/// Throws <see cref="ServiceUnavailableException"/> when Redis operations fail.
/// </summary>
public class RedisCartService : ICartService
{
    private readonly IDatabase _db;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisCartService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCartService(IDatabase db, RedisSettings settings, ILogger<RedisCartService> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    private static string CartKey(Guid userId) => $"cart:{userId}";

    /// <inheritdoc/>
    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        try
        {
            var key = CartKey(userId);
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return EmptyCart(userId);

            var cart = JsonSerializer.Deserialize<CartDto>((string)value!, SerializerOptions)
                       ?? EmptyCart(userId);
            _logger.LogInformation("[CartService] Retrieved cart for user {UserId} ({Count} items).", userId, cart.Items.Count);
            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CartService] GetCartAsync failed for user {UserId}.", userId);
            throw new ServiceUnavailableException("Cart storage is unavailable. Please try again later.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CartDto> AddItemAsync(Guid userId, CartItemDto snapshot)
    {
        try
        {
            var cart = await GetCartInternalAsync(userId);
            var existing = cart.Items.FirstOrDefault(i => i.VariantId == snapshot.VariantId);
            if (existing is not null)
            {
                existing.Quantity += snapshot.Quantity;
                existing.LineTotal = existing.UnitPrice * existing.Quantity;
            }
            else
            {
                snapshot.LineTotal = snapshot.UnitPrice * snapshot.Quantity;
                cart.Items.Add(snapshot);
            }

            cart.GrandTotal = cart.Items.Sum(i => i.LineTotal);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            cart.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(_settings.CartTtlSeconds);

            await PersistCartAsync(userId, cart);
            _logger.LogInformation("[CartService] Added/merged variant {VariantId} for user {UserId}.", snapshot.VariantId, userId);
            return cart;
        }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CartService] AddItemAsync failed for user {UserId}.", userId);
            throw new ServiceUnavailableException("Cart storage is unavailable. Please try again later.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CartDto> UpdateItemQuantityAsync(Guid userId, Guid variantId, int quantity)
    {
        try
        {
            var cart = await GetCartInternalAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId)
                       ?? throw new NotFoundException("Cart item", variantId);

            if (quantity == 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.LineTotal = item.UnitPrice * quantity;
            }

            cart.GrandTotal = cart.Items.Sum(i => i.LineTotal);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            cart.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(_settings.CartTtlSeconds);

            await PersistCartAsync(userId, cart);
            _logger.LogInformation("[CartService] Updated variant {VariantId} qty={Qty} for user {UserId}.", variantId, quantity, userId);
            return cart;
        }
        catch (NotFoundException) { throw; }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CartService] UpdateItemQuantityAsync failed for user {UserId}.", userId);
            throw new ServiceUnavailableException("Cart storage is unavailable. Please try again later.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid variantId)
    {
        try
        {
            var cart = await GetCartInternalAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId)
                       ?? throw new NotFoundException("Cart item", variantId);

            cart.Items.Remove(item);
            cart.GrandTotal = cart.Items.Sum(i => i.LineTotal);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            cart.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(_settings.CartTtlSeconds);

            await PersistCartAsync(userId, cart);
            _logger.LogInformation("[CartService] Removed variant {VariantId} for user {UserId}.", variantId, userId);
            return cart;
        }
        catch (NotFoundException) { throw; }
        catch (ServiceUnavailableException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CartService] RemoveItemAsync failed for user {UserId}.", userId);
            throw new ServiceUnavailableException("Cart storage is unavailable. Please try again later.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task ClearCartAsync(Guid userId)
    {
        try
        {
            await _db.KeyDeleteAsync(CartKey(userId));
            _logger.LogInformation("[CartService] Cart cleared for user {UserId}.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CartService] ClearCartAsync failed for user {UserId}.", userId);
            throw new ServiceUnavailableException("Cart storage is unavailable. Please try again later.", ex);
        }
    }

    private async Task<CartDto> GetCartInternalAsync(Guid userId)
    {
        var key = CartKey(userId);
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return EmptyCart(userId);
        return JsonSerializer.Deserialize<CartDto>((string)value!, SerializerOptions) ?? EmptyCart(userId);
    }

    private async Task PersistCartAsync(Guid userId, CartDto cart)
    {
        var json = JsonSerializer.Serialize(cart, SerializerOptions);
        var ttl = TimeSpan.FromSeconds(_settings.CartTtlSeconds);
        await _db.StringSetAsync(CartKey(userId), json, ttl);
    }

    private static CartDto EmptyCart(Guid userId) => new()
    {
        UserId = userId,
        Items = new List<CartItemDto>(),
        GrandTotal = 0m,
        UpdatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
    };
}
