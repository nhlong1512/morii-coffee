using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.Commands.Cart.AddItemToCart;

/// <summary>
/// Handles <see cref="AddItemToCartCommand"/> by validating that the product (and optional variant)
/// exist in the database, building a price-snapshot <see cref="CartItemDto"/>, and delegating to
/// <see cref="ICartService"/> to persist the item in Redis.
/// </summary>
public class AddItemToCartCommandHandler : ICommandHandler<AddItemToCartCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;
    private readonly AwsS3Settings _s3Settings;

    /// <summary>Initialises the handler with its required dependencies.</summary>
    public AddItemToCartCommandHandler(IUnitOfWork unitOfWork, ICartService cartService, AwsS3Settings s3Settings)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
        _s3Settings = s3Settings;
    }

    /// <summary>
    /// Executes the add-to-cart operation: loads the product and optional variant from the
    /// database, builds the cart item with a price snapshot, and stores it in Redis.
    /// </summary>
    public async Task<bool> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product is null || product.IsDeleted)
            throw new NotFoundException("Product", request.ProductId);

        decimal additionalPrice = 0;
        string? variantLabel = null;

        if (request.VariantId.HasValue)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(request.VariantId.Value)
                ?? throw new NotFoundException("ProductVariant", request.VariantId.Value);

            additionalPrice = variant.AdditionalPrice;
            variantLabel = variant.Name;
        }

        var item = new CartItemDto
        {
            ProductId = request.ProductId,
            VariantId = request.VariantId,
            VariantLabel = variantLabel,
            ProductName = product.Name,
            UnitPrice = product.BasePrice + additionalPrice,
            Quantity = request.Quantity,
            ImageUrl = CdnUrlHelper.Resolve(product.ThumbnailUrl, _s3Settings.CdnBaseUrl),
            AddedAt = DateTime.UtcNow
        };

        await _cartService.AddItemAsync(request.UserId, item);

        return true;
    }
}
