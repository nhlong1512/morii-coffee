using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.MergeGuestCart;

/// <summary>
/// Command to merge localStorage guest cart items into the authenticated user's Redis cart.
/// Sent immediately after login. Items with the same ProductId + VariantId have quantities summed.
/// </summary>
public class MergeGuestCartCommand : ICommand<bool>
{
    /// <summary>ID of the authenticated user whose cart will receive the guest items (set from JWT claims).</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// List of cart items from the guest (unauthenticated) session to merge into the user's cart.
    /// </summary>
    public List<CartItemDto> GuestItems { get; set; } = [];
}
