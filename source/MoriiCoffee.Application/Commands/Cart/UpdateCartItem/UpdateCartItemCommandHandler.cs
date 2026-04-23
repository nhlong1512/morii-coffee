using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Cart.UpdateCartItem;

/// <summary>Updates the quantity of an existing cart line. Quantity 0 removes the line.</summary>
public class UpdateCartItemCommandHandler : ICommandHandler<UpdateCartItemCommand, CartDto>
{
    private readonly ICartService _cartService;

    public UpdateCartItemCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken) =>
        _cartService.UpdateItemQuantityAsync(request.UserId, request.VariantId, request.Quantity);
}
