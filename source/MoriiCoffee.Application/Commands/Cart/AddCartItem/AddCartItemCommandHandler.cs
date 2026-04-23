using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Cart.AddCartItem;

/// <summary>
/// Validates the variant exists, builds a snapshotted cart item, and delegates to the cart service.
/// If the variant is already in the cart, the service accumulates the quantity rather than duplicating the line.
/// </summary>
public class AddCartItemCommandHandler : ICommandHandler<AddCartItemCommand, CartDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;

    public AddCartItemCommandHandler(IUnitOfWork unitOfWork, ICartService cartService)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
    }

    public async Task<CartDto> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(request.VariantId)
            ?? throw new NotFoundException("ProductVariant", request.VariantId);

        var product = await _unitOfWork.Products.GetByIdAsync(variant.ProductId)
            ?? throw new NotFoundException("Product", variant.ProductId);

        var snapshot = new CartItemDto
        {
            ProductId = product.Id,
            VariantId = variant.Id,
            ProductName = product.Name,
            VariantName = variant.Name,
            ThumbnailUrl = product.ThumbnailUrl,
            UnitPrice = product.BasePrice + variant.AdditionalPrice,
            Quantity = request.Quantity,
            LineTotal = (product.BasePrice + variant.AdditionalPrice) * request.Quantity
        };

        return await _cartService.AddItemAsync(request.UserId, snapshot);
    }
}
