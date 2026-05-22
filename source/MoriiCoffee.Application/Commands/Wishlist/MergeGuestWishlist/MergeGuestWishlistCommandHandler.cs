using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Wishlist;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Wishlist.MergeGuestWishlist;

/// <summary>
/// Merges guest productIds into the user's server wishlist (idempotent per product).
/// Returns the full merged wishlist with live product snapshots.
/// </summary>
public class MergeGuestWishlistCommandHandler : ICommandHandler<MergeGuestWishlistCommand, WishlistDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AwsS3Settings _s3Settings;

    public MergeGuestWishlistCommandHandler(IUnitOfWork unitOfWork, AwsS3Settings s3Settings)
    {
        _unitOfWork = unitOfWork;
        _s3Settings = s3Settings;
    }

    public async Task<WishlistDto> Handle(MergeGuestWishlistCommand request, CancellationToken cancellationToken)
    {
        if (request.GuestProductIds.Count > 0)
        {
            var existing = await _unitOfWork.WishlistItems
                .GetByUserIdAsync(request.UserId);

            var existingProductIds = existing.Select(i => i.ProductId).ToHashSet();

            var newIds = request.GuestProductIds
                .Distinct()
                .Where(id => !existingProductIds.Contains(id))
                .ToList();

            if (newIds.Count > 0)
            {
                var validProductIds = await _unitOfWork.Products
                    .FindByCondition(p => newIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                foreach (var productId in validProductIds)
                {
                    await _unitOfWork.WishlistItems.AddAsync(new WishlistItem
                    {
                        UserId = request.UserId,
                        ProductId = productId,
                        AddedAt = DateTime.UtcNow,
                    });
                }

                await _unitOfWork.CommitAsync();
            }
        }

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

        return new WishlistDto { Items = items, UpdatedAt = updatedAt };
    }
}
