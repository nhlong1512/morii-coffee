using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.RemoveCartItem;

/// <summary>Removes a product variant line from the authenticated user's cart.</summary>
public class RemoveCartItemCommandHandler : ICommandHandler<RemoveCartItemCommand, CartDto>
{
    private readonly ICartService _cartService;

    public RemoveCartItemCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public Task<CartDto> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken) =>
        _cartService.RemoveItemAsync(request.UserId, request.VariantId);
}
