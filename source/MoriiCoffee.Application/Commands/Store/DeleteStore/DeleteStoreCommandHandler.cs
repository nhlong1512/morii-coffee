using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Store.DeleteStore;

/// <summary>Handles <see cref="DeleteStoreCommand"/> by soft-deleting the store.</summary>
public class DeleteStoreCommandHandler : ICommandHandler<DeleteStoreCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteStoreCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _unitOfWork.Stores.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Store", request.Id);

        await _unitOfWork.Stores.SoftDelete(store);
        await _unitOfWork.CommitAsync();
        return true;
    }
}
