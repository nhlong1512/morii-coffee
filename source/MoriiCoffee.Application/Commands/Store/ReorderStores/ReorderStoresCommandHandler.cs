using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Store.ReorderStores;

/// <summary>
/// Handles <see cref="ReorderStoresCommand"/> by updating DisplayOrder for each listed store in a single commit.
/// </summary>
public class ReorderStoresCommandHandler : ICommandHandler<ReorderStoresCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReorderStoresCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(ReorderStoresCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(item.Id)
                ?? throw new NotFoundException("Store", item.Id);

            store.SetDisplayOrder(item.DisplayOrder);
            await _unitOfWork.Stores.Update(store);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }
}
