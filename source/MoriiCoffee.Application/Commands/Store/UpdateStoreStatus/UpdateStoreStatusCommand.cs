using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Store.UpdateStoreStatus;

/// <summary>Command to toggle the IsActive flag of a store without a full update.</summary>
public class UpdateStoreStatusCommand : ICommand<StoreDto>
{
    public UpdateStoreStatusCommand(Guid id, UpdateStoreStatusDto dto)
    {
        Id = id;
        IsActive = dto.IsActive;
    }

    /// <summary>ID of the store whose status should be updated.</summary>
    public Guid Id { get; }

    /// <summary>The desired active status.</summary>
    public bool IsActive { get; }
}
