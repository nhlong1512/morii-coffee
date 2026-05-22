using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Wishlist;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Wishlist.GetWishlist;

/// <summary>
/// Returns the user's wishlist with live product snapshots joined from the Products table.
/// inStock is derived from product.Status — Active = true, Inactive/OutOfStock = false.
/// </summary>
public class GetWishlistQueryHandler : IQueryHandler<GetWishlistQuery, WishlistDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AwsS3Settings _s3Settings;

    public GetWishlistQueryHandler(IUnitOfWork unitOfWork, AwsS3Settings s3Settings)
    {
        _unitOfWork = unitOfWork;
        _s3Settings = s3Settings;
    }

    public async Task<WishlistDto> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var wishlistItems = await _unitOfWork.WishlistItems.GetByUserIdAsync(request.UserId);

        if (wishlistItems.Count == 0)
            return new WishlistDto();

        var productIds = wishlistItems.Select(w => w.ProductId).ToHashSet();

        var products = await _unitOfWork.Products
            .FindByCondition(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = wishlistItems
            .Where(w => products.ContainsKey(w.ProductId))
            .Select(w =>
            {
                var product = products[w.ProductId];
                return new WishlistItemDto
                {
                    ProductId = product.Id.ToString(),
                    ProductName = product.Name,
                    ProductSlug = product.Slug,
                    BasePrice = product.BasePrice,
                    ThumbnailUrl = CdnUrlHelper.Resolve(product.ThumbnailUrl, _s3Settings.CdnBaseUrl),
                    InStock = product.Status == EProductStatus.Active,
                    AddedAt = w.AddedAt,
                };
            })
            .ToList();

        var updatedAt = wishlistItems.Count > 0
            ? wishlistItems.Max(w => w.AddedAt)
            : (DateTime?)null;

        return new WishlistDto
        {
            Items = items,
            UpdatedAt = updatedAt,
        };
    }
}
