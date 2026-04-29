using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.MergeGuestCart;

/// <summary>
/// Handles <see cref="MergeGuestCartCommand"/> by delegating to <see cref="ICartService"/>
/// to merge guest cart items into the authenticated user's Redis cart.
/// Items sharing the same ProductId + VariantId will have their quantities summed.
/// </summary>
public class MergeGuestCartCommandHandler : ICommandHandler<MergeGuestCartCommand, bool>
{
    private readonly ICartService _cartService;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public MergeGuestCartCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Merges guest cart items into the authenticated user's Redis cart.</summary>
    public async Task<bool> Handle(MergeGuestCartCommand request, CancellationToken cancellationToken)
    {
        await _cartService.MergeAsync(request.UserId, request.GuestItems);

        return true;
    }
}
