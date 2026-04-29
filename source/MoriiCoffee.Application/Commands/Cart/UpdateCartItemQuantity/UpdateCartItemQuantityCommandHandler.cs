using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.UpdateCartItemQuantity;

/// <summary>
/// Handles <see cref="UpdateCartItemQuantityCommand"/> by delegating to
/// <see cref="ICartService"/> to update the quantity of the specified line item.
/// Setting quantity to 0 removes the item.
/// </summary>
public class UpdateCartItemQuantityCommandHandler : ICommandHandler<UpdateCartItemQuantityCommand, bool>
{
    private readonly ICartService _cartService;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public UpdateCartItemQuantityCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Updates the quantity of the matching cart item, removing it when quantity is zero.</summary>
    public async Task<bool> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        await _cartService.UpdateQuantityAsync(request.UserId, request.ProductId, request.VariantId, request.Quantity);

        return true;
    }
}
