using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Cart.GetCart;

/// <summary>
/// Handles <see cref="GetCartQuery"/> by retrieving the user's current cart state from Redis
/// via <see cref="ICartService"/>. Returns an empty cart when no cart exists for the user.
/// </summary>
public class GetCartQueryHandler : IQueryHandler<GetCartQuery, CartDto>
{
    private readonly ICartService _cartService;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public GetCartQueryHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Returns the current cart for the authenticated user.</summary>
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        return await _cartService.GetCartAsync(request.UserId);
    }
}
