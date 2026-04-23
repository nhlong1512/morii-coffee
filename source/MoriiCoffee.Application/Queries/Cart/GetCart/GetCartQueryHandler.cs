using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Cart.GetCart;

/// <summary>Returns the authenticated user's active cart from Redis.</summary>
public class GetCartQueryHandler : IQueryHandler<GetCartQuery, CartDto>
{
    private readonly ICartService _cartService;

    public GetCartQueryHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken) =>
        _cartService.GetCartAsync(request.UserId);
}
