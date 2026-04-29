using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.ClearCart;

/// <summary>
/// Handles <see cref="ClearCartCommand"/> by delegating to <see cref="ICartService"/>
/// to delete the entire Redis cart entry for the user.
/// </summary>
public class ClearCartCommandHandler : ICommandHandler<ClearCartCommand, bool>
{
    private readonly ICartService _cartService;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public ClearCartCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Clears all items from the user's Redis cart.</summary>
    public async Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        await _cartService.ClearCartAsync(request.UserId);

        return true;
    }
}
