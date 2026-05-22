using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Wishlist.AddItemToWishlist;

/// <summary>
/// Adds a product to the user's wishlist.
/// Idempotent — if the product is already wishlisted, returns true without error.
/// </summary>
public class AddItemToWishlistCommandHandler : ICommandHandler<AddItemToWishlistCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddItemToWishlistCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(AddItemToWishlistCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product is null)
            throw new NotFoundException("Product", request.ProductId);

        var alreadyExists = await _unitOfWork.WishlistItems.ExistsAsync(request.UserId, request.ProductId);
        if (alreadyExists)
            return true;

        var item = new WishlistItem
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            AddedAt = DateTime.UtcNow,
        };

        await _unitOfWork.WishlistItems.AddAsync(item);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
