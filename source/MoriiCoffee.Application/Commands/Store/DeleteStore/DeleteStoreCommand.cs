using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Store.DeleteStore;

/// <summary>Command to soft-delete a store by setting its IsDeleted flag.</summary>
public class DeleteStoreCommand : ICommand<bool>
{
    public DeleteStoreCommand(Guid id) => Id = id;

    /// <summary>ID of the store to soft-delete.</summary>
    public Guid Id { get; }
}
