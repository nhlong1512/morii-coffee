using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.RemoveItemFromCart;

/// <summary>
/// Handles <see cref="RemoveItemFromCartCommand"/> by delegating directly to
/// <see cref="ICartService"/> to remove the specified line item from the Redis cart.
/// </summary>
public class RemoveItemFromCartCommandHandler : ICommandHandler<RemoveItemFromCartCommand, bool>
{
    private readonly ICartService _cartService;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public RemoveItemFromCartCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Removes the matching product/variant line item from the user's cart.</summary>
    public async Task<bool> Handle(RemoveItemFromCartCommand request, CancellationToken cancellationToken)
    {
        await _cartService.RemoveItemAsync(request.UserId, request.ProductId, request.VariantId);

        return true;
    }
}
