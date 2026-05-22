using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Wishlist.RemoveItemFromWishlist;

/// <summary>Removes a product from the user's wishlist. Throws 404 if not found.</summary>
public class RemoveItemFromWishlistCommandHandler : ICommandHandler<RemoveItemFromWishlistCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveItemFromWishlistCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveItemFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var removed = await _unitOfWork.WishlistItems.RemoveAsync(request.UserId, request.ProductId);

        if (!removed)
            throw new NotFoundException("Wishlist item", $"userId={request.UserId}, productId={request.ProductId}");

        await _unitOfWork.CommitAsync();
        return true;
    }
}
