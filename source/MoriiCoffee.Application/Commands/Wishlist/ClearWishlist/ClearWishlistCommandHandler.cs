using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Wishlist.ClearWishlist;

/// <summary>Removes all items from the user's wishlist.</summary>
public class ClearWishlistCommandHandler : ICommandHandler<ClearWishlistCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ClearWishlistCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ClearWishlistCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.WishlistItems.ClearAsync(request.UserId);
        await _unitOfWork.CommitAsync();
        return true;
    }
}
