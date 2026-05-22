using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Store.UpdateStoreStatus;

/// <summary>Handles <see cref="UpdateStoreStatusCommand"/> by updating only the IsActive flag.</summary>
public class UpdateStoreStatusCommandHandler : ICommandHandler<UpdateStoreStatusCommand, StoreDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateStoreStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StoreDto> Handle(UpdateStoreStatusCommand request, CancellationToken cancellationToken)
    {
        var store = await _unitOfWork.Stores.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Store", request.Id);

        store.SetStatus(request.IsActive);
        await _unitOfWork.Stores.Update(store);
        await _unitOfWork.CommitAsync();
        return _mapper.Map<StoreDto>(store);
    }
}
