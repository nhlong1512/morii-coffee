using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.ClearCart;

/// <summary>Deletes the authenticated user's entire cart document from Redis.</summary>
public class ClearCartCommandHandler : ICommandHandler<ClearCartCommand, bool>
{
    private readonly ICartService _cartService;

    public ClearCartCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        await _cartService.ClearCartAsync(request.UserId);
        return true;
    }
}
